// This file is released under MIT license.
// See file LICENSE.txt for more information.

#include <ilcp/cp.h>
#include <iostream>
#include <algorithm>
#include <map>
#include <omp.h>
#include "../input/readers/CppInputReader.h"
#include "SolverConfig.h"
#include "ConstructiveHeuristic.h"
#include "../datastructs/FixedPermCostComputation.h"
#include "../datastructs/GcdOfValues.h"
#include "../datastructs/Block.h"
#include "../algorithms/PackToBlocksByCp.h"

using namespace std;
using namespace escs;

int main(int /*argc*/, char **argv) {
    cout << "In cpp" << endl;

    auto solverConfigPath = string(argv[1]);
    auto specializedSolverConfigPath = string(argv[2]);
    auto instancePath = string(argv[3]);
    auto resultPath = string(argv[4]);

    auto solverConfig = SolverConfig::ReadFromPath(solverConfigPath);
    auto specializedSolverConfig = ConstructiveHeuristic::SpecializedSolverConfig::ReadFromPath(specializedSolverConfigPath);

    CppInputReader inputReader;
    auto instance = inputReader.readFromPath(instancePath);

    ConstructiveHeuristic solver(instance, solverConfig, specializedSolverConfig);
    solver.solve();

    auto result = solver.getResult();
    result.writeToPath(resultPath);

    return 0;
}

namespace escs {
    ConstructiveHeuristic::ConstructiveHeuristic(
            const escs::Instance &instance,
            escs::SolverConfig &solverConfig,
            const ConstructiveHeuristic::SpecializedSolverConfig &specializedSolverConfig)
                : mInstance(instance), mSolverConfig(solverConfig), mSpecializedSolverConfig(specializedSolverConfig) {
    }

    Status ConstructiveHeuristic::solve() {
        mStopwatch.start();
        this->solveInternal();
        mStopwatch.stop();

        if (mObj.has_value()) {
            mStatus = Status::Heuristic;
        }
        else {
            mStatus = Status::NoSolution;
        }

        return mStatus;
    }

    void ConstructiveHeuristic::solveInternal() {
        mStatus = Status::NoSolution;
        mProcTimesPerm = vector<int>();
        mStartTimesPerm = vector<int>();
        mObj = optional<int>();

        if (mSolverConfig.mNumWorkers >= 1) {
            // Currently, only FixedPermCostComputation runs in parallel and uses OpenMP.
            omp_set_num_threads(mSolverConfig.mNumWorkers);
        }

        switch (mSpecializedSolverConfig.mAlgorithm) {
            case AllPositions:
                algorithmAllPositions();
                return;
            case AllPositionsWithBlockKeeping:
                algorithmAllPositionsWithBlockKeeping();
                return;
            default:
                throw invalid_argument("Invalid algorithm");
        }
    }

    vector<int> ConstructiveHeuristic::getProcessingTimesOrdering() {
        vector<int> processingTimes;
        for (auto &pJob : mInstance.mJobs) {
            processingTimes.push_back(pJob->mProcessingTime);
        }

        switch (mSpecializedSolverConfig.mJobsOrdering) {
            case Random:
                shuffle(processingTimes.begin(), processingTimes.end(), mSolverConfig.mRandom);
                break;

            case LongestProcessingTimeFirst:
                sort(processingTimes.begin(), processingTimes.end());
                reverse(processingTimes.begin(), processingTimes.end());
                break;

            case ShortestProcessingTimeFirst:
                sort(processingTimes.begin(), processingTimes.end());
                break;

            case AlternateShortestLongestProcessingTime:
                {
                    sort(processingTimes.begin(), processingTimes.end());
                    vector<int> newProcessingTimes;
                    int left = 0;
                    int right = (int)processingTimes.size() - 1;
                    while (left <= right) {
                        newProcessingTimes.push_back(processingTimes[left]);
                        if (left < right) {
                            newProcessingTimes.push_back(processingTimes[right]);
                        }

                        left++;
                        right--;
                    }

                    processingTimes = newProcessingTimes;
                }
                break;

            case AlternateHalvesShortLongProcessingTime:
                {
                    sort(processingTimes.begin(), processingTimes.end());
                    vector<int> newProcessingTimes;
                    int half = processingTimes.size() / 2;
                    int left = 0;
                    int right = half;
                    while (left < half || right < (int)processingTimes.size()) {
                        if (left < half) {
                            newProcessingTimes.push_back(processingTimes[left]);
                        }

                        if (right < (int)processingTimes.size()) {
                            newProcessingTimes.push_back(processingTimes[right]);
                        }

                        left++;
                        right++;
                    }

                    processingTimes = newProcessingTimes;
                }
                break;
        }

        return processingTimes;
    }

    void ConstructiveHeuristic::algorithmAllPositions() {
        auto remainingProcTimes = getProcessingTimesOrdering();

        vector<int> currProcTimesPerm;
        for (auto procTimeToInsert : remainingProcTimes) {
            cout << "Iteration " << currProcTimesPerm.size() << endl;

            if (currProcTimesPerm.size() == 0) {
                currProcTimesPerm.push_back(procTimeToInsert);
                continue;
            }

            optional<int> bestPosition;
            optional<int> bestCost;
            for (int insertPosition = 0; insertPosition <= (int)currProcTimesPerm.size(); insertPosition++) {
                // Symmetry breaking on equal processing times.
                if (insertPosition > 0 && currProcTimesPerm[insertPosition - 1] == procTimeToInsert) {
                    continue;
                }

                auto procTimesWithInsertion = currProcTimesPerm;
                procTimesWithInsertion.insert(procTimesWithInsertion.begin() + insertPosition, procTimeToInsert);

                auto [cost, _] = computeObjective(
                        mInstance,
                        procTimesWithInsertion);

                if (cost.has_value()) {
                    if (!bestCost.has_value() || (cost.value() < bestCost.value())) {
                        bestPosition = insertPosition;
                        bestCost = cost;
                    }
                }
            }

            if (!bestCost.has_value()) {
                // Cannot find solution with the current partial permutation.
                cout << "Cannot find solution" << endl;
                return;
            }

            currProcTimesPerm.insert(currProcTimesPerm.begin() + bestPosition.value(), procTimeToInsert);
        }

        mProcTimesPerm = currProcTimesPerm;
        auto [cost, startTimes] = computeObjective(
                mInstance,
                mProcTimesPerm);
        mStartTimesPerm = startTimes;
        mObj = cost;
    }

    void ConstructiveHeuristic::algorithmAllPositionsWithBlockKeeping() {
        auto remainingProcTimes = getProcessingTimesOrdering();

        vector<int> currBlocksPerm;
        int iteration = 0;
        for (auto procTimeToInsert : remainingProcTimes) {
            cout << "Iteration " << iteration << endl;

            if (currBlocksPerm.size() == 0) {
                currBlocksPerm.push_back(procTimeToInsert);
                continue;
            }

            optional<int> bestPosition;
            optional<int> bestCost;
            for (int insertPosition = 0; insertPosition <= (int)currBlocksPerm.size(); insertPosition++) {
                auto procTimesWithInsertion = currBlocksPerm;
                procTimesWithInsertion.insert(procTimesWithInsertion.begin() + insertPosition, procTimeToInsert);

                auto [cost, _] = computeObjective(
                        mInstance,
                        procTimesWithInsertion);

                if (cost.has_value()) {
                    if (!bestCost.has_value() || (cost.value() < bestCost.value())) {
                        bestPosition = insertPosition;
                        bestCost = cost;
                    }
                }
            }

            if (!bestCost.has_value()) {
                // Cannot find solution with the current partial permutation.
                cout << "Cannot find solution" << endl;
                return;
            }

            currBlocksPerm.insert(currBlocksPerm.begin() + bestPosition.value(), procTimeToInsert);
            auto [_, blockStartTimes] = computeObjective(
                    mInstance,
                    currBlocksPerm);
            auto blocks = Block::getProcBlocks(blockStartTimes, currBlocksPerm, 0);
            currBlocksPerm.clear();
            for (auto &block : blocks) {
                currBlocksPerm.push_back(block.getLength());
            }

            iteration++;
        }
        auto [cost, blockStartTimes] = computeObjective(
                mInstance,
                currBlocksPerm);

        PackToBlocksByCp packToBlocksByCp;
        packToBlocksByCp.solve(
                Block::getProcBlocks(blockStartTimes, currBlocksPerm, 0),
                remainingProcTimes,
                optional<chrono::milliseconds>());

        mProcTimesPerm = packToBlocksByCp.mPermProcTimes;
        mStartTimesPerm = packToBlocksByCp.mPermStartTimes;
        mObj = cost;
    }

    pair<optional<int>, vector<int>> ConstructiveHeuristic::computeObjective(
            const Instance &instance,
            vector<int> procTimes) {
        int totalProcTime = 0;
        for (int procTime : procTimes) {
            totalProcTime += procTime;
        }

        FixedPermCostComputation costComputation(
                totalProcTime,
                instance.mIntervals.size(),
                instance.mEarliestOnIntervalIdx,
                instance.mLatestOnIntervalIdx,
                instance.mOnPowerConsumption,
                instance.mFullOptimalSwitchingCosts,
                instance.mCumulativeEnergyCost,
                vector<bool>(instance.mIntervals.size(), true));

        for (int position = 0; position < (int)procTimes.size(); position++) {
            costComputation.join(position, procTimes[position]);
        }

        int cost = costComputation.recomputeCost();
        if (cost == Instance::NO_VALUE) {
            return make_pair(optional<int>(), vector<int>());
        }
        else {
            return make_pair(optional<int>(cost), costComputation.reconstructStartTimes());
        }
    }

    vector<int> ConstructiveHeuristic::getStartTimes() const {
        map<int, vector<const Job*>> jobsByProcTime;
        for (auto *pJob : mInstance.mJobs) {
            auto it = jobsByProcTime.find(pJob->mProcessingTime);
            if (it == jobsByProcTime.end()) {
                jobsByProcTime[pJob->mProcessingTime] = vector<const Job*> {pJob};
            }
            else {
                it->second.push_back(pJob);
            }
        }

        vector<int> startTimes(mInstance.mJobs.size(), Instance::NO_VALUE);
        for (int position = 0; position < (int)mProcTimesPerm.size(); position++) {
            int procTime = mProcTimesPerm[position];
            int startTime = mStartTimesPerm[position];
            auto *pJob = jobsByProcTime[procTime].back();
            jobsByProcTime[procTime].pop_back();
            startTimes[pJob->mIndex] = startTime;
        }

        return startTimes;
    }

    Result ConstructiveHeuristic::getResult() const {
        return Result(
                mStatus,
                mStopwatch.timeLimitReached(mSolverConfig.mTimeLimit),
                mObj,
                getStartTimes());
    }

    ConstructiveHeuristic::SpecializedSolverConfig::SpecializedSolverConfig(
            Algorithm algorithm,
            JobsOrdering jobsOrdering,
            int randomPositionsCount)
                : mAlgorithm(algorithm),
                  mJobsOrdering(jobsOrdering),
                  mRandomPositionsCount(randomPositionsCount) {
    }

    ConstructiveHeuristic::SpecializedSolverConfig ConstructiveHeuristic::SpecializedSolverConfig::ReadFromPath(string specializedSolverConfigPath) {
        ifstream stream;
        stream.open(specializedSolverConfigPath);

        int algorithm;
        stream >> algorithm;

        int jobsOrdering;
        stream >> jobsOrdering;

        int randomPositionsCount;
        stream >> randomPositionsCount;

        return SpecializedSolverConfig(
                (Algorithm)algorithm,
                (JobsOrdering)jobsOrdering,
                randomPositionsCount);
    }
}

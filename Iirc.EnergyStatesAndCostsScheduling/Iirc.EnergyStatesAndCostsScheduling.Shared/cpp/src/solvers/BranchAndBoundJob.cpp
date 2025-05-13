// This file is released under MIT license.
// See file LICENSE.txt for more information.

#include <ilcp/cp.h>
#include <iostream>
#include <algorithm>
#include <map>
#include <omp.h>
#include <random>
#include "../input/readers/CppInputReader.h"
#include "SolverConfig.h"
#include "BranchAndBoundJob.h"
#include "../datastructs/FixedPermCostComputation.h"
#include "../datastructs/GcdOfValues.h"
#include "../datastructs/Block.h"
#include "../algorithms/BlockFinding.h"
#include <gurobi_c++.h>

using namespace std;
using namespace escs;

int main(int /*argc*/, char **argv) {
    cout << "In cpp" << endl;

    auto solverConfigPath = string(argv[1]);
    auto specializedSolverConfigPath = string(argv[2]);
    // TODO: would be great to use in BaB? Or do we really need it?
    auto instancePath = string(argv[3]);
    auto resultPath = string(argv[4]);

    auto solverConfig = SolverConfig::ReadFromPath(solverConfigPath);
    auto specializedSolverConfig = BranchAndBoundOnJob::SpecializedSolverConfig::ReadFromPath(specializedSolverConfigPath);

    CppInputReader inputReader;
    auto instance = inputReader.readFromPath(instancePath);

    // Both iterative deepining and simple BaB need all processable intervals at the beginning.
    vector<bool> processableIntervals = vector<bool>(instance.mIntervals.size(), true);
    solverConfig.mProcessableIntervals = processableIntervals;

    if (specializedSolverConfig.mUseIterativeDeepening)
    {
        BranchAndBoundOnJob::SpecializedSolverConfig iterativeDeepeningSpecializedSolverConfig(
                specializedSolverConfig.mUsePrimalHeuristicBlockDetection,
                specializedSolverConfig.mUsePrimalHeuristicPackToBlocksByCp,
                specializedSolverConfig.mPrimalHeuristicPackToBlocksByCpAllJobs,
                specializedSolverConfig.mUseIterativeDeepening,
                specializedSolverConfig.mBlockFinding,
                specializedSolverConfig.mBlockFindingStrategy,
                specializedSolverConfig.mJobsJoiningOnGcd,
                specializedSolverConfig.mBranchPriority,
                specializedSolverConfig.mIterativeDeepeningTimeLimit,
                optional<long long>());

        if (iterativeDeepeningSpecializedSolverConfig.mIterativeDeepeningTimeLimit.has_value()) {
            auto timeLimit = solverConfig.mTimeLimit;

            // Run iterative deepening with specific time limit.
            SolverConfig iterativeDeepeningConfig(
                    uniform_int_distribution<>()(solverConfig.mRandom),
                    iterativeDeepeningSpecializedSolverConfig.mIterativeDeepeningTimeLimit,
                    solverConfig.mNumWorkers,
                    vector<int>());
            iterativeDeepeningConfig.mProcessableIntervals = processableIntervals;

            auto resultIterativeDeepening = iterativeDeeping(iterativeDeepeningConfig, iterativeDeepeningSpecializedSolverConfig, instance);
            if (resultIterativeDeepening.mStatus == Status::Optimal) {
                // Optimal means all intervals were processable.
                resultIterativeDeepening.writeToPath(resultPath);
            }
            else {
                SolverConfig babConfig(
                uniform_int_distribution<>()(solverConfig.mRandom),
                        timeLimit.has_value() ? timeLimit.value() - iterativeDeepeningSpecializedSolverConfig.mIterativeDeepeningTimeLimit.value() : timeLimit,
                        solverConfig.mNumWorkers,
                        resultIterativeDeepening.mStatus == Status::Heuristic || resultIterativeDeepening.mStatus == Status::Optimal ? resultIterativeDeepening.mStartTimes : vector<int>());
                babConfig.mProcessableIntervals = processableIntervals;
                BranchAndBoundOnJob solver(instance, babConfig, specializedSolverConfig);
                solver.solve();

                auto result = solver.getResult();
                result.writeToPath(resultPath);
            }
        }
        else {
            auto result = iterativeDeeping(solverConfig, iterativeDeepeningSpecializedSolverConfig, instance);
            result.writeToPath(resultPath);
        }
    }
    else {
        BranchAndBoundOnJob solver(instance, solverConfig, specializedSolverConfig);
        solver.solve();

        auto result = solver.getResult();
        result.writeToPath(resultPath);
    }

    return 0;
}

namespace escs {
    Result iterativeDeeping(
            SolverConfig &solverConfig,
            const BranchAndBoundOnJob::SpecializedSolverConfig &specializedSolverConfig,
            const Instance &instance) {

        // Find the initial relaxed blocks.
        FixedPermCostComputation initialRelaxedBlocksComputation(
                instance.getTotalProcTime(),
                instance.mIntervals.size(),
                instance.mEarliestOnIntervalIdx,
                instance.mLatestOnIntervalIdx,
                instance.mOnPowerConsumption,
                instance.mOptimalSwitchingCosts,
                instance.mCumulativeEnergyCost,
                solverConfig.mProcessableIntervals);
        if (specializedSolverConfig.mJobsJoiningOnGcd == BranchAndBoundOnJob::JobsJoiningOnGcd::ROOT
            || specializedSolverConfig.mJobsJoiningOnGcd == BranchAndBoundOnJob::JobsJoiningOnGcd::WHOLE_TREE) {
            vector<int> allProcTimes;
            for (auto *pJob : instance.mJobs) {
                allProcTimes.push_back(pJob->mProcessingTime);
            }
            GcdOfValues gcdOfValues(allProcTimes);
            if (instance.getTotalProcTime() > 0) {
                int joinedGcd = gcdOfValues.gcd(allProcTimes);
                if (joinedGcd != 1) {
                    initialRelaxedBlocksComputation.setProcTimes(0, joinedGcd);
                }
            }
        }

        if (initialRelaxedBlocksComputation.recomputeCost() == Instance::NO_VALUE) {
            // Infeasible.
            return Result(Status::Infeasible, false);
        }

        auto relaxedProcBlocks = Block::getProcBlocks(initialRelaxedBlocksComputation, 0);

        auto stopwatch = Stopwatch();
        stopwatch.start();

        // Aggregated statistics over iterations.
        long long totalNodesCount = 0;
        long long totalPrimalHeuristicBlockDetectionFoundSolution = 0;
        long long totalPrimalHeuristicPackToBlocksByCpFoundSolution = 0;
        long long totalJobsJoinedOnLargerGcd = 0;
        chrono::milliseconds totalLowerBoundTotalDuration = chrono::milliseconds::zero();
        chrono::milliseconds totalPrimalHeuristicBLockDetectionDuration = chrono::milliseconds::zero();
        chrono::milliseconds totalPrimalHeuristicPackToBlocksByCpDuration = chrono::milliseconds::zero();
        chrono::milliseconds totalPrimalHeuristicBLockFindingDuration = chrono::milliseconds::zero();

        int currPuffSize = 2;
        optional<int> currObj;
        vector<int> currStartTimes;
        while (!stopwatch.timeLimitReached(solverConfig.mTimeLimit)) {
            // Puff the blocks.
            auto currProcessableIntervals = puffBlocksToProcessableIntervals(instance, relaxedProcBlocks, currPuffSize);
            bool allIntervalsAreProcessable = true;
            for (int intervalIdx = instance.mEarliestOnIntervalIdx; intervalIdx <= instance.mLatestOnIntervalIdx; intervalIdx++) {
                if (!currProcessableIntervals[intervalIdx]) {
                    allIntervalsAreProcessable = false;
                    break;
                }
            }
            cout << "Current puff size: " << currPuffSize << ", all intervals processable? " << allIntervalsAreProcessable << endl;

            // Solve the BaB. Use new time-limit, current processable intervals and initial start times from the
            // previous iteration.
            SolverConfig currSolverConfig(
                    uniform_int_distribution<>()(solverConfig.mRandom),
                    stopwatch.remainingTime(solverConfig.mTimeLimit),
                    solverConfig.mNumWorkers,
                    currStartTimes);
            currSolverConfig.mProcessableIntervals = currProcessableIntervals;

            BranchAndBoundOnJob solver(instance, currSolverConfig, specializedSolverConfig);
            solver.solve();

            auto currResult = solver.getResult();

            // Aggregation.
            if (currResult.mNodesCount.has_value()) {
                totalNodesCount += currResult.mNodesCount.value();
            }
            if (currResult.mPrimalHeuristicBlockDetectionFoundSolution.has_value()) {
                totalPrimalHeuristicBlockDetectionFoundSolution += currResult.mPrimalHeuristicBlockDetectionFoundSolution.value();
            }
            if (currResult.mPrimalHeuristicPackToBlocksByCpFoundSolution.has_value()) {
                totalPrimalHeuristicPackToBlocksByCpFoundSolution += currResult.mPrimalHeuristicPackToBlocksByCpFoundSolution.value();
            }
            if (currResult.mJobsJoinedOnLargerGcd.has_value()) {
                totalJobsJoinedOnLargerGcd += currResult.mJobsJoinedOnLargerGcd.value();
            }
            if (currResult.mLowerBoundTotalDuration.has_value()) {
                totalLowerBoundTotalDuration += currResult.mLowerBoundTotalDuration.value();
            }
            if (currResult.mPrimalHeuristicBlockDetectionTotalDuration.has_value()) {
                totalPrimalHeuristicBLockDetectionDuration += currResult.mPrimalHeuristicBlockDetectionTotalDuration.value();
            }
            if (currResult.mPrimalHeuristicPackToBlockByCpTotalDuration.has_value()) {
                totalPrimalHeuristicPackToBlocksByCpDuration += currResult.mPrimalHeuristicPackToBlockByCpTotalDuration.value();
            }
            if (currResult.mPrimalHeuristicBlockFindingTotalDuration.has_value()) {
                totalPrimalHeuristicBLockFindingDuration += currResult.mPrimalHeuristicBlockFindingTotalDuration.value();
            }

            switch (currResult.mStatus) {
                case Status::Infeasible:
                    if (allIntervalsAreProcessable) {
                        // Whole problem solved.
                        return Result(
                                Status::Infeasible,
                                false,
                                optional<int>(),
                                vector<int>(),
                                totalNodesCount,
                                totalPrimalHeuristicBlockDetectionFoundSolution,
                                totalPrimalHeuristicPackToBlocksByCpFoundSolution,
                                totalJobsJoinedOnLargerGcd,
                                optional<int>(),
                                totalLowerBoundTotalDuration,
                                totalPrimalHeuristicBLockDetectionDuration,
                                totalPrimalHeuristicPackToBlocksByCpDuration,
                                totalPrimalHeuristicBLockFindingDuration);
                    }
                    else {
                        // Cannot decide, needs another puffing.
                    }
                    break;

                case Status::Heuristic:
                    assert(currResult.mTimeLimitReached);
                    return Result(
                            Status::Heuristic,
                            true,
                            currResult.mObjective,
                            currResult.mStartTimes,
                            totalNodesCount,
                            totalPrimalHeuristicBlockDetectionFoundSolution,
                            totalPrimalHeuristicPackToBlocksByCpFoundSolution,
                            totalJobsJoinedOnLargerGcd,
                            initialRelaxedBlocksComputation.getOptCost(),
                            totalLowerBoundTotalDuration,
                            totalPrimalHeuristicBLockDetectionDuration,
                            totalPrimalHeuristicPackToBlocksByCpDuration,
                            totalPrimalHeuristicBLockFindingDuration);
                    break;

                case Status::Optimal:
                    if (allIntervalsAreProcessable) {
                        // Whole problem solved.
                        return Result(
                                Status::Optimal,
                                false,
                                currResult.mObjective,
                                currResult.mStartTimes,
                                totalNodesCount,
                                totalPrimalHeuristicBlockDetectionFoundSolution,
                                totalPrimalHeuristicPackToBlocksByCpFoundSolution,
                                totalJobsJoinedOnLargerGcd,
                                initialRelaxedBlocksComputation.getOptCost(),
                                totalLowerBoundTotalDuration,
                                totalPrimalHeuristicBLockDetectionDuration,
                                totalPrimalHeuristicPackToBlocksByCpDuration,
                                totalPrimalHeuristicBLockFindingDuration);
                    }
                    else {
                        // Cannot decide, needs another puffing.
                        currObj = currResult.mObjective;
                        currStartTimes = currResult.mStartTimes;
                    }
                    break;

                case Status::NoSolution:
                    assert(currResult.mTimeLimitReached);
                    return Result(
                            currObj.value() ? Status::Heuristic : Status::NoSolution,
                            true,
                            currObj,
                            currStartTimes,
                            totalNodesCount,
                            totalPrimalHeuristicBlockDetectionFoundSolution,
                            totalPrimalHeuristicPackToBlocksByCpFoundSolution,
                            totalJobsJoinedOnLargerGcd,
                            initialRelaxedBlocksComputation.getOptCost(),
                            totalLowerBoundTotalDuration,
                            totalPrimalHeuristicBLockDetectionDuration,
                            totalPrimalHeuristicPackToBlocksByCpDuration,
                            totalPrimalHeuristicBLockFindingDuration);
            }

            // Need another iteration, puff intervals.
            currPuffSize *= 2;
        }

        return Result(
                currObj.value() ? Status::Heuristic : Status::NoSolution,
                true,
                currObj,
                currStartTimes,
                totalNodesCount,
                totalPrimalHeuristicBlockDetectionFoundSolution,
                totalPrimalHeuristicPackToBlocksByCpFoundSolution,
                totalJobsJoinedOnLargerGcd,
                initialRelaxedBlocksComputation.getOptCost(),
                totalLowerBoundTotalDuration,
                totalPrimalHeuristicBLockDetectionDuration,
                totalPrimalHeuristicPackToBlocksByCpDuration,
                totalPrimalHeuristicBLockFindingDuration);
    }

    vector<bool> puffBlocksToProcessableIntervals(
            const Instance &instance,
            const vector<Block> &blocks,
            int puffSize) {
        auto starts = vector<int>(blocks.size(), 0);
        auto completions = vector<int>(blocks.size(), 0);

        for (int blockIdx = 0; blockIdx < (int) blocks.size(); blockIdx++) {
            auto block = blocks[blockIdx];
            int leftStart = max(block.mStart - puffSize, instance.mEarliestOnIntervalIdx);
            int rightEnd = min(instance.mLatestOnIntervalIdx + 1, block.mCompletion + puffSize);

            starts[blockIdx] = leftStart;
            completions[blockIdx] = rightEnd;
        }

        auto processableIntervals = vector<bool>(instance.mIntervals.size(), false);
        for (int blockIdx = 0; blockIdx < (int) blocks.size(); blockIdx++) {
            for (int intervalIdx = starts[blockIdx]; intervalIdx < completions[blockIdx]; intervalIdx++) {
                processableIntervals[intervalIdx] = true;
            }
        }

        return processableIntervals;
    }

    BranchAndBoundOnJob::BranchAndBoundOnJob(
            const escs::Instance &instance,
            escs::SolverConfig &solverConfig,
            const BranchAndBoundOnJob::SpecializedSolverConfig &specializedSolverConfig)
                : mInstance(instance), mSolverConfig(solverConfig), mSpecializedSolverConfig(specializedSolverConfig), mRandomBranchPriorityDist(0, 1) {
    }

    Status BranchAndBoundOnJob::solve() {
        mStopwatch.start();
        this->solveInternal();
        mStopwatch.stop();

        if (mStopwatch.timeLimitReached(mSolverConfig.mTimeLimit) || mNodesCountLimitReached) {
            if (mCurrBestObj.has_value()) {
                mStatus = Status::Heuristic;
            }
            else {
                mStatus = Status::NoSolution;
            }
        }
        else {
            if (mCurrBestObj.has_value()) {
                mStatus = Status::Optimal;
            }
            else {
                mStatus = Status::Infeasible;
            }
        }

        return mStatus;
    }

    void BranchAndBoundOnJob::solveInternal() {
        mNodesCountLimitReached = false;
        mNodesCount = 0;
        mPrimalHeuristicBlockDetectionFoundSolution = 0;
        mUsePrimalHeuristicPackToBlocksByCpFoundSolution = 0;
        mJobsJoinedOnLargerGcd = 0;
        mCurrBestObj.reset();
        mStatus = Status::NoSolution;

        if (mSolverConfig.mNumWorkers >= 1) {
            // Currently, only FixedPermCostComputation runs in parallel and uses OpenMP.
            omp_set_num_threads(mSolverConfig.mNumWorkers);
        }

        vector<vector<int>> fixedProcTimesBlocks;
        vector<int> allProcTimes;
        map<int, int> remainingProcTimeCounts;
        for (auto *pJob : mInstance.mJobs) {
            allProcTimes.push_back(pJob->mProcessingTime);
            auto it = remainingProcTimeCounts.find(pJob->mProcessingTime);
            if (it == remainingProcTimeCounts.end()) {
                remainingProcTimeCounts[pJob->mProcessingTime] = 1;
            }
            else {
                it->second++;
            }
        }
        FixedPermCostComputation fixedPermCostComputation(
                mInstance.getTotalProcTime(),
                mInstance.mIntervals.size(),
                mInstance.mEarliestOnIntervalIdx,
                mInstance.mLatestOnIntervalIdx,
                mInstance.mOnPowerConsumption,
                mInstance.mOptimalSwitchingCosts,
                mInstance.mCumulativeEnergyCost,
                mSolverConfig.mProcessableIntervals);

        if (!mSolverConfig.mInitialStartTimes.empty()) {
            vector<pair<int, int>> procTimeWithStart; // (procTime, startTime)
            for (auto pJob : mInstance.mJobs) {
                procTimeWithStart.push_back(make_pair(pJob->mProcessingTime, mSolverConfig.mInitialStartTimes.at(pJob->mIndex)));
            }

            sort(
                    procTimeWithStart.begin(),
                    procTimeWithStart.end(),
                    [&](const pair<int, int> &lhs, const pair<int, int> &rhs) {
                        return lhs.second < rhs.second;
                    });

            for (int position = 0; position < (int)procTimeWithStart.size(); position++) {
                fixedPermCostComputation.join(position, procTimeWithStart[position].first);
            }

            mCurrBestObj = fixedPermCostComputation.recomputeCost();
            mCurrBestPermStartTimes = fixedPermCostComputation.reconstructStartTimes();
            mCurrBestPermProcTimes = vector<int>();
            for (int position = 0; position < (int)procTimeWithStart.size(); position++) {
                mCurrBestPermProcTimes.push_back(procTimeWithStart[position].first);
            }

            fixedPermCostComputation.reset();

            cout << "BAB initialized with objective " << mCurrBestObj.value() << endl;
        }

        mFixedBlocksComputation.reset(new FixedPermCostComputation(
                mInstance.getTotalProcTime(),
                mInstance.mIntervals.size(),
                mInstance.mEarliestOnIntervalIdx,
                mInstance.mLatestOnIntervalIdx,
                mInstance.mOnPowerConsumption,
                mInstance.mOptimalSwitchingCosts,
                mInstance.mCumulativeEnergyCost,
                mSolverConfig.mProcessableIntervals));

        GcdOfValues gcdOfValues(allProcTimes);

        int currJoinedGcd = 1;
        if (mSpecializedSolverConfig.mJobsJoiningOnGcd == ROOT
            || mSpecializedSolverConfig.mJobsJoiningOnGcd == WHOLE_TREE)
        {
            if (mInstance.getTotalProcTime() > 0) {
                currJoinedGcd = gcdOfValues.gcd(allProcTimes);
                if (currJoinedGcd != 1) {
                    mJobsJoinedOnLargerGcd++;
                    fixedPermCostComputation.setProcTimes(0, currJoinedGcd);
                }
            }
        }

        this->enterNode(
                fixedProcTimesBlocks,
                0,
                remainingProcTimeCounts,
                mInstance.getTotalProcTime(),
                fixedPermCostComputation,
                gcdOfValues,
                currJoinedGcd,
                optional<int>(),
                vector<Block>(),
                false);

        mLowerBoundTotalDuration = fixedPermCostComputation.getCostComputationTotalDuration();
        mPrimalHeuristicBlockDetectionTotalDuration = mPrimalHeuristicBlockDetectionStopwatch.totalDuration();
        mPrimalHeuristicPackToBlocksByCpTotalDuration = mPrimalHeuristicPackToBlocksByCpStopwatch.totalDuration();
        mPrimalHeuristicBlockFindingTotalDuration = mPrimalHeuristicBlockFindingStopwatch.totalDuration();
    }

    void BranchAndBoundOnJob::enterNode(
            vector<vector<int>> &fixedProcTimesBlocks,
            int fixedProcTimesCount,
            map<int, int> &remainingProcTimeCounts,
            int remainingProcTime,
            FixedPermCostComputation &fixedPermCostComputation,
            GcdOfValues &gcdOfValues,
            int currJoinedGcd,
            optional<int> inheritedLowerBound,
            vector<Block> remProcBlocksReversed,
            bool joinToPrevBlock) {
        if (mSpecializedSolverConfig.mFullHorizonBabNodesCountLimit.has_value()
            && mNodesCount >= mSpecializedSolverConfig.mFullHorizonBabNodesCountLimit.value()) {
            mNodesCountLimitReached = true;
            return;
        }

        mNodesCount++;
        long long currNode = mNodesCount;
        mCurrNode = currNode;

#ifdef DEBUG
        printCurrNodeLogPrefix(fixedProcTimesBlocks); cout << "Node " << currNode << " entered." << endl;
        printCurrNodeLogPrefix(fixedProcTimesBlocks); cout << "currJoinedGcd: " << currJoinedGcd << endl;
#endif
        int currNodeLowerBound;
        if (inheritedLowerBound.has_value()) {
            currNodeLowerBound = inheritedLowerBound.value();
        }
        else {
            currNodeLowerBound = fixedPermCostComputation.recomputeCost();
            if (currNodeLowerBound == Instance::NO_VALUE) {
#ifdef DEBUG
                printCurrNodeLogPrefix(fixedProcTimesBlocks); cout << "Not feasible node based on LB" << endl;
#endif
                return;
            }
            remProcBlocksReversed = Block::getProcBlocks(fixedPermCostComputation, fixedProcTimesBlocks.size());
            reverse(remProcBlocksReversed.begin(), remProcBlocksReversed.end());
        }

#ifdef DEBUG
        printCurrNodeLogPrefix(fixedProcTimesBlocks); cout << "LB: " << currNodeLowerBound << endl;
        printCurrNodeLogPrefix(fixedProcTimesBlocks); cout << "Inherited LB? " << inheritedLowerBound.has_value() << endl;
#endif

        if (currNode == 1) {
            mRootLowerBound = currNodeLowerBound;
        }

        // Check lower bound
        if (mCurrBestObj.has_value()) {
            if (mCurrBestObj.value() <= currNodeLowerBound) {
#ifdef DEBUG
                printCurrNodeLogPrefix(fixedProcTimesBlocks); cout << "Pruning node with lb=" << currNodeLowerBound << " by ub=" << mCurrBestObj.value() << endl;
#endif
                return;
            }
        }

        // Everything scheduled?
        if (remainingProcTime == 0) {
            if (currNodeLowerBound != Instance::NO_VALUE) {
                if (!mCurrBestObj.has_value() || currNodeLowerBound < mCurrBestObj.value()) {
                    mCurrBestObj = currNodeLowerBound;
                    mCurrBestPermProcTimes = this->flatten(fixedProcTimesBlocks);
                    mCurrBestPermStartTimes = this->startTimesFromBlockProcTimes(
                            fixedPermCostComputation.reconstructStartTimes(),
                            fixedProcTimesBlocks);
#ifdef DEBUG
                    printCurrNodeLogPrefix(fixedProcTimesBlocks);
#endif
                    cout << "New ub (leaf): " << mCurrBestObj.value() << ", time " << mStopwatch.totalDuration().count() << " ms " << endl;
                }
            }

            return;
        }

        // Primal heuristics.
        // If we inherited lower bound from parent node, do not compute them (as they would not find any new solution).
        if (!inheritedLowerBound.has_value()) {
            // Primal heuristic: block detection.
            if (mSpecializedSolverConfig.mUsePrimalHeuristicBlockDetection) {
                if (this->PerformPrimalHeuristicBlockDetection(fixedProcTimesBlocks, remainingProcTimeCounts,
                                                               remainingProcTime,
                                                               fixedPermCostComputation)) {
                    return;
                }
            }

            // Primal heuristic: packing of remaining proctimes into blocks (using CP).
            if (mSpecializedSolverConfig.mUsePrimalHeuristicPackToBlocksByCp) {
                if (this->PerformPrimalHeuristicPackToBlocksByCp(fixedProcTimesBlocks, remainingProcTimeCounts,
                                                                 remainingProcTime,
                                                                 fixedPermCostComputation)) {
                    return;
                }
            }

            // Primal heuristic: trying to reconstruct UB using block-finding model
            if (((mSpecializedSolverConfig.mBlockFinding == PrimalHeuristicBlockFinding::BF_ROOT) && (currNode == 1))
                || (mSpecializedSolverConfig.mBlockFinding == PrimalHeuristicBlockFinding::BF_WHOLE_TREE)) {
                mPrimalHeuristicBlockFindingStopwatch.start();
                bool sameAsRelaxedBlocks = false;
                auto assignments = PerformPrimalHeuristicBlockFinding(Block::getProcBlocks(fixedPermCostComputation, 0), sameAsRelaxedBlocks);

                if (assignments.size() > 0) {
                    mFixedBlocksComputation->reset();

                    vector<int> newBlockLengths;
                    for (int j = 0; j < (int)mInstance.mJobs.size(); j++) {
                        auto assignment = assignments[j];
                        if ((int)newBlockLengths.size() <= assignment) {
                            newBlockLengths.resize(assignment + 1, 0);
                        }
                        newBlockLengths[assignment] += mInstance.mJobs[j]->mProcessingTime;
                    }

                    for (int position = 0; position < (int)newBlockLengths.size(); position++) {
                        mFixedBlocksComputation->join(position, newBlockLengths[position]);
                    }
                    auto newUpperBound = mFixedBlocksComputation->recomputeCost();

                    if (newUpperBound != Instance::NO_VALUE
                        && (!mCurrBestObj.has_value() || mCurrBestObj.value() > newUpperBound)) {
                        mCurrBestObj = newUpperBound;

                        auto newBlockStartTimes = mFixedBlocksComputation->reconstructStartTimes();

                        // TODO: merge this with the logic from CP?
                        vector<pair<int, int>> remainingProcTimeWithStart; // (procTime, startTime)
                        for (int j = 0; j < (int)mInstance.mJobs.size(); j++) {
                            auto blockIdx = assignments[j];
                            int startTime = newBlockStartTimes[blockIdx];
                            int procTime = mInstance.mJobs[j]->mProcessingTime;
                            remainingProcTimeWithStart.push_back(make_pair(procTime, startTime));
                            newBlockStartTimes[blockIdx] = startTime + procTime;
                        }

                        sort(
                                remainingProcTimeWithStart.begin(),
                                remainingProcTimeWithStart.end(),
                                [&](const pair<int, int> &lhs, const pair<int, int> &rhs) {
                                    return lhs.second < rhs.second;
                                });

                        mCurrBestPermStartTimes = vector<int>();
                        mCurrBestPermProcTimes = vector<int>();
                        for (auto &p : remainingProcTimeWithStart) {
                            mCurrBestPermProcTimes.push_back(p.first);
                            mCurrBestPermStartTimes.push_back(p.second);
                        }

#ifdef DEBUG
                        printCurrNodeLogPrefix(fixedProcTimesBlocks);
#endif
                        cout << "New ub (PrimalHeuristicBlockFinding): " << mCurrBestObj.value() << ", time " << mStopwatch.totalDuration().count() << " ms " << endl;

                        if (sameAsRelaxedBlocks) {
                            mPrimalHeuristicBlockFindingStopwatch.stop();
                            return;
                        }
                    }
                }

                mPrimalHeuristicBlockFindingStopwatch.stop();
            }
        }

        // Branching.
        for (auto &procTimeAndCount : remainingProcTimeCounts) {
            int procTime = procTimeAndCount.first;
            int remainingProcTimeCount = procTimeAndCount.second;
            if (remainingProcTimeCount == 0) {
                continue;
            }

            if (joinToPrevBlock && fixedProcTimesBlocks.back().back() > procTime) {
                // Only non-decreasing jobs joining.
                continue;
            }

            auto randomBranchType = mRandomBranchPriorityDist(mSolverConfig.mRandom);
            for (int branchType = 0; branchType <= 1; branchType++) {   // The actual type is determined by forcedSpace.
                bool forcedSpace = false;
                switch (mSpecializedSolverConfig.mBranchPriority) {
                    case Random:
                        forcedSpace = randomBranchType == branchType;
                        break;

                    case ForcedSpace:
                        forcedSpace = branchType == 0;
                        break;

                    case JoinToPrev:
                        forcedSpace = branchType != 0;
                        break;

                    case DynamicByBlockFitting:
                        if (remProcBlocksReversed.back().getLength() >= procTime) {
                            forcedSpace = branchType == 1;
                        }
                        else {
                            forcedSpace = branchType == 0;
                        }
                        break;
                }

#ifdef DEBUG
                printCurrNodeLogPrefix(fixedProcTimesBlocks); cout << "Fixing proctime " << procTime << ", forcing next space " << forcedSpace << endl;
#endif

                if (joinToPrevBlock) {
                    fixedProcTimesBlocks.back().push_back(procTime);
                }
                else {
                    fixedProcTimesBlocks.push_back({ procTime });
                }
                fixedProcTimesCount++;

                remainingProcTimeCount--;
                procTimeAndCount.second = remainingProcTimeCount;
                int newRemainingProcTime = remainingProcTime - procTime;

                if (joinToPrevBlock) {
                    fixedPermCostComputation.join(fixedProcTimesBlocks.size() - 1, 1 + procTime / currJoinedGcd);
                }
                else {
                    fixedPermCostComputation.join(fixedProcTimesBlocks.size() - 1, procTime / currJoinedGcd);
                }

                // Set forced space.
                fixedPermCostComputation.setForcedSpace(fixedProcTimesBlocks.size() - 1, forcedSpace ? 1 : 0);

                int newJoinedGcd = currJoinedGcd;
                if (mSpecializedSolverConfig.mJobsJoiningOnGcd == WHOLE_TREE) {
                    if (newRemainingProcTime > 0) {
                        // TODO (perf):
                        vector<int> remainingProcTimes;
                        for (auto it : remainingProcTimeCounts) {
                            for (int i = 0; i < it.second; i++) {
                                remainingProcTimes.push_back(it.first);
                            }
                        }

                        newJoinedGcd = gcdOfValues.gcd(remainingProcTimes);
                        if (newJoinedGcd != currJoinedGcd) {
                            if (newJoinedGcd > currJoinedGcd) {
                                mJobsJoinedOnLargerGcd++;
                            }
                            fixedPermCostComputation.setProcTimes(fixedProcTimesBlocks.size(), newJoinedGcd);
                        }
                    }
                }

                // Lower bound inheritance.
                optional<int> childInheritedLowerBound;
                vector<Block> childRemProcBlocksReversed;
                if (!forcedSpace) { // No inheritance when forcing space.
                    if (remProcBlocksReversed.back().getLength() >= procTime) {
                        childInheritedLowerBound = currNodeLowerBound;
                        childRemProcBlocksReversed = remProcBlocksReversed;
                        childRemProcBlocksReversed.back().mStart += procTime;
                        if (childRemProcBlocksReversed.back().getLength() == 0) {
                            childRemProcBlocksReversed.pop_back();
                        }
                    }
                }

                // Go deeper.
                this->enterNode(
                        fixedProcTimesBlocks,
                        fixedProcTimesCount,
                        remainingProcTimeCounts,
                        newRemainingProcTime,
                        fixedPermCostComputation,
                        gcdOfValues,
                        newJoinedGcd,
                        childInheritedLowerBound,
                        childRemProcBlocksReversed,
                        !forcedSpace);

                mCurrNode = currNode;

                // Undo new gcd splits into the old one.
                if (mSpecializedSolverConfig.mJobsJoiningOnGcd == WHOLE_TREE) {
                    if (newRemainingProcTime > 0 && newJoinedGcd != currJoinedGcd) {
                        fixedPermCostComputation.setProcTimes(fixedProcTimesBlocks.size(), currJoinedGcd);
                    }
                }

                // Undo forced space.
                fixedPermCostComputation.setForcedSpace(fixedProcTimesBlocks.size() - 1, 0);
                if (fixedProcTimesBlocks.size() >= 2) {
                    fixedPermCostComputation.setForcedSpace(fixedProcTimesBlocks.size() - 2, joinToPrevBlock ? 0 : 1);
                }

                // Undo join.
                if (joinToPrevBlock) {
                    // TODO (perf):
                    int lastBlockTotalProcTime = 0;
                    for (auto p : fixedProcTimesBlocks.back()) {
                        lastBlockTotalProcTime += p;
                    }

                    vector<int> newSplits;
                    newSplits.push_back(lastBlockTotalProcTime - procTime);
                    int procTimeSplitsCount = procTime / currJoinedGcd;
                    for (int i = 0; i < procTimeSplitsCount; i++) {
                        newSplits.push_back(currJoinedGcd);
                    }

                    fixedPermCostComputation.split(fixedProcTimesBlocks.size() - 1, newSplits);
                }
                else {
                    fixedPermCostComputation.split(fixedProcTimesBlocks.size() - 1, procTime / currJoinedGcd);
                }

                remainingProcTimeCount++;
                procTimeAndCount.second = remainingProcTimeCount;

                if (joinToPrevBlock) {
                    fixedProcTimesBlocks.back().pop_back();
                }
                else {
                    fixedProcTimesBlocks.pop_back();
                }

                fixedProcTimesCount--;

                // Check lb again.
                if (mCurrBestObj.has_value()) {
                    if (mCurrBestObj.value() <= currNodeLowerBound) {
#ifdef DEBUG
                        printCurrNodeLogPrefix(fixedProcTimesBlocks); cout << "Pruning node (by backtrack) with lb=" << currNodeLowerBound << " by ub=" << mCurrBestObj.value() << endl;
#endif
                        return;
                    }
                }

                if (stopSearching()) {
                    return;
                }
            }
        }
    }

    vector<int> BranchAndBoundOnJob::getStartTimes() const {
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
        for (int position = 0; position < (int)mCurrBestPermProcTimes.size(); position++) {
            int procTime = mCurrBestPermProcTimes[position];
            int startTime = mCurrBestPermStartTimes[position];
            auto *pJob = jobsByProcTime[procTime].back();
            jobsByProcTime[procTime].pop_back();
            startTimes[pJob->mIndex] = startTime;
        }

        return startTimes;
    }

    Result BranchAndBoundOnJob::getResult() const {
        return Result(
                mStatus,
                mStopwatch.timeLimitReached(mSolverConfig.mTimeLimit),
                mCurrBestObj,
                getStartTimes(),
                mNodesCount,
                mPrimalHeuristicBlockDetectionFoundSolution,
                mUsePrimalHeuristicPackToBlocksByCpFoundSolution,
                mJobsJoinedOnLargerGcd,
                mRootLowerBound,
                mLowerBoundTotalDuration,
                mPrimalHeuristicBlockDetectionTotalDuration,
                mPrimalHeuristicPackToBlocksByCpTotalDuration,
                mPrimalHeuristicBlockFindingTotalDuration);
    }


    vector<int> BranchAndBoundOnJob::PerformPrimalHeuristicBlockFinding(
            const vector<Block> &relaxedBlocks,
            bool &sameAsRelaxedBlocks) {
        BlockFinding blockFinding(mEnv);
        blockFinding.solve(
                (BlockFinding::BlockFindingStrategy)mSpecializedSolverConfig.mBlockFindingStrategy,
                mInstance,
                relaxedBlocks,
                chrono::milliseconds(5000));
        sameAsRelaxedBlocks = blockFinding.mSolutionSameAsBlocks;
        return blockFinding.mAssignments;
    }

    bool BranchAndBoundOnJob::PerformPrimalHeuristicBlockDetection(
            vector<vector<int>> &fixedProcTimesBlocks,
            map<int, int> &remainingProcTimeCounts,
            int remainingProcTime,
            FixedPermCostComputation &fixedPermCostComputation)
    {
        mPrimalHeuristicBlockDetectionStopwatch.start();

        fixedPermCostComputation.recomputeCost();

        if (mCurrBestObj.has_value()) {
            if (mCurrBestObj.value() <= fixedPermCostComputation.getOptCost()) {
                mPrimalHeuristicBlockDetectionStopwatch.stop();
                return false;
            }
        }

        auto startTimes = fixedPermCostComputation.reconstructStartTimes();
        int blockStart = startTimes[fixedProcTimesBlocks.size()];
        int blockCompletion = startTimes.back() + fixedPermCostComputation.getPermProcTimes().back();

        bool blockDetected = false;
        if ((blockCompletion - blockStart) == remainingProcTime) {
            blockDetected = true;

            if (!mCurrBestObj.has_value() || fixedPermCostComputation.getOptCost() < mCurrBestObj.value()) {
                mCurrBestObj = fixedPermCostComputation.getOptCost();

                mCurrBestPermProcTimes = this->flatten(fixedProcTimesBlocks);
                mCurrBestPermStartTimes = this->startTimesFromBlockProcTimes(startTimes, fixedProcTimesBlocks);

                int nextStartTime = blockStart;
                for (auto it : remainingProcTimeCounts) {
                    int procTime = it.first;
                    for (int i = 0; i < it.second; i++) {
                        mCurrBestPermStartTimes.push_back(nextStartTime);
                        mCurrBestPermProcTimes.push_back(procTime);
                        nextStartTime += procTime;
                    }
                }

#ifdef DEBUG
                printCurrNodeLogPrefix(fixedProcTimesBlocks);
#endif
                cout << "New ub (PerformPrimalHeuristicBlockDetection): " << mCurrBestObj.value() << ", time " << mStopwatch.totalDuration().count() << " ms " << endl;
                mPrimalHeuristicBlockDetectionFoundSolution++;
            }
        }

        mPrimalHeuristicBlockDetectionStopwatch.stop();
        return blockDetected;
    }

    bool BranchAndBoundOnJob::PerformPrimalHeuristicPackToBlocksByCp(
            vector<vector<int>> &fixedProcTimesBlocks,
            map<int, int> &remainingProcTimeCounts,
            int /*remainingProcTime*/,
            FixedPermCostComputation &fixedPermCostComputation)
    {
        mPrimalHeuristicPackToBlocksByCpStopwatch.start();

        fixedPermCostComputation.recomputeCost();
        if (mCurrBestObj.has_value()) {
            if (mCurrBestObj.value() <= fixedPermCostComputation.getOptCost()) {
                mPrimalHeuristicPackToBlocksByCpStopwatch.stop();
                return false;
            }
        }

        auto blocks = Block::getProcBlocks(
            fixedPermCostComputation,
            mSpecializedSolverConfig.mPrimalHeuristicPackToBlocksByCpAllJobs ? 0 : fixedProcTimesBlocks.size());

#ifdef DEBUG
        printCurrNodeLogPrefix(fixedProcTimesBlocks);
        cout << "Block sizes for cp: ";
        for (auto &block : blocks) {
            cout << block.mStart << "=|" << block.getLength() << "|, ";
        }
        cout << endl;
#endif

        // Construct CP model.
        IloEnv env;
        IloModel model(env);

        IloIntVarArray load(env);
        for (auto &block : blocks) {
            load.add(IloIntVar(env, block.getLength()));
        }

        IloIntArray size(env);
        if (mSpecializedSolverConfig.mPrimalHeuristicPackToBlocksByCpAllJobs) {
            for (auto pJob : mInstance.mJobs) {
                size.add(pJob -> mProcessingTime);
            }
        }
        else {
            for (auto it : remainingProcTimeCounts) {
                int procTime = it.first;
                for (int i = 0; i < it.second; i++) {
                    size.add(procTime);
                }
            }
        }

#ifdef DEBUG
        {
            int totalProcTime = 0;
            int totalBlockLength = 0;
            for (int i = 0; i < size.getSize(); i++) {
                totalProcTime += size[i];
            }

            for (auto &block : blocks) {
                totalBlockLength += block.getLength();
            }

            assert(totalBlockLength == totalProcTime);
        }
#endif

        IloIntVarArray where(env, size.getSize(), 0, load.getSize() - 1);
        model.add(IloPack(env, load, where, size));

        IloCP cp(model);

        // Parameters.
        auto remainingTimeLimit = mStopwatch.remainingTime(mSolverConfig.mTimeLimit);
        if (remainingTimeLimit.has_value() && remainingTimeLimit.value().count() > 0) {
            float timeLimitInSeconds = (1.0 * remainingTimeLimit.value().count()) / 1000.0;
            cp.setParameter(IloCP::TimeLimit, timeLimitInSeconds);
        }
        if (mSolverConfig.mNumWorkers > 0) {
            cp.setParameter(IloCP::Workers, mSolverConfig.mNumWorkers);
        }
        cp.setParameter(IloCP::LogVerbosity, IloCP::Quiet);
        if (cp.solve()) {
            // Solution found, reconstruct start times.
#ifdef DEBUG
            printCurrNodeLogPrefix(fixedProcTimesBlocks);
#endif
            cout << "New ub (PerformPrimalHeuristicPackToBlocksByCp): " << fixedPermCostComputation.getOptCost() << ", time " << mStopwatch.totalDuration().count() << " ms " << endl;
            mUsePrimalHeuristicPackToBlocksByCpFoundSolution++;

            mCurrBestObj = fixedPermCostComputation.getOptCost();

            vector<int> blockNextStarts;
            for (auto &block : blocks) {
                blockNextStarts.push_back(block.mStart);
            }

            vector<pair<int, int>> remainingProcTimeWithStart; // (procTime, startTime)
            for (int i = 0; i < size.getSize(); i++) {
                int procTime = size[i];
                int blockIdx = cp.getValue(where[i]);
                int startTime = blockNextStarts[blockIdx];
                remainingProcTimeWithStart.push_back(make_pair(procTime, startTime));
                blockNextStarts[blockIdx] = startTime + procTime;
            }

#ifdef DEBUG
            // Check that all blocks were filled.
            for (int blockIdx = 0; blockIdx < (int)blocks.size(); blockIdx++) {
                assert(blockNextStarts[blockIdx] == blocks[blockIdx].mCompletion);
            }
#endif
            sort(
                    remainingProcTimeWithStart.begin(),
                    remainingProcTimeWithStart.end(),
                    [&](const pair<int, int> &lhs, const pair<int, int> &rhs) {
                        return lhs.second < rhs.second;
                    });

            mCurrBestPermStartTimes = vector<int>();
            mCurrBestPermProcTimes = vector<int>();
            if (!mSpecializedSolverConfig.mPrimalHeuristicPackToBlocksByCpAllJobs) {
                mCurrBestPermProcTimes = this->flatten(fixedProcTimesBlocks);
                mCurrBestPermStartTimes = this->startTimesFromBlockProcTimes(
                    fixedPermCostComputation.reconstructStartTimes(),
                     fixedProcTimesBlocks);
            }
            for (auto &p : remainingProcTimeWithStart) {
                mCurrBestPermProcTimes.push_back(p.first);
                mCurrBestPermStartTimes.push_back(p.second);
            }

            env.end();
            mPrimalHeuristicPackToBlocksByCpStopwatch.stop();
            return true;
        }

        env.end();
        mPrimalHeuristicPackToBlocksByCpStopwatch.stop();
        return false;
    }

    BranchAndBoundOnJob::SpecializedSolverConfig::SpecializedSolverConfig(
            bool usePrimalHeuristicBlockDetection,
            bool usePrimalHeuristicPackToBlocksByCp,
            bool primalHeuristicPackToBlocksByCpAllJobs,
            bool useIterativeDeepening,
            PrimalHeuristicBlockFinding blockFinding,
            PrimalHeuristicBlockFindingStrategy blockFindingStrategy,
            JobsJoiningOnGcd jobsJoiningOnGcd,
            BranchPriority branchPriority,
            optional<chrono::milliseconds> iterativeDeepeningTimeLimit,
            optional<long long> fullHorizonBabNodesCountLimit)
            : mUsePrimalHeuristicBlockDetection(usePrimalHeuristicBlockDetection),
                  mUsePrimalHeuristicPackToBlocksByCp(usePrimalHeuristicPackToBlocksByCp),
                  mPrimalHeuristicPackToBlocksByCpAllJobs(primalHeuristicPackToBlocksByCpAllJobs),
                  mUseIterativeDeepening(useIterativeDeepening),
                  mBlockFinding(blockFinding),
                  mBlockFindingStrategy(blockFindingStrategy),
                  mJobsJoiningOnGcd(jobsJoiningOnGcd),
                  mBranchPriority(branchPriority),
                  mIterativeDeepeningTimeLimit(iterativeDeepeningTimeLimit),
                  mFullHorizonBabNodesCountLimit(fullHorizonBabNodesCountLimit) {
    }

    BranchAndBoundOnJob::SpecializedSolverConfig BranchAndBoundOnJob::SpecializedSolverConfig::ReadFromPath(string specializedSolverConfigPath) {
        ifstream stream;
        stream.open(specializedSolverConfigPath);

        int usePrimalHeuristicBlockDetection;
        stream >> usePrimalHeuristicBlockDetection;

        int usePrimalHeuristicPackToBlocksByCp;
        stream >> usePrimalHeuristicPackToBlocksByCp;

        int primalHeuristicPackToBlocksByCpAllJobs;
        stream >> primalHeuristicPackToBlocksByCpAllJobs;

        int useIterativeDeepening;
        stream >> useIterativeDeepening;

        int blockFinding;
        stream >> blockFinding;

        int blockFindingStrategy;
        stream >> blockFindingStrategy;

        int jobsJoiningOnGcd;
        stream >> jobsJoiningOnGcd;

        int branchPriority;
        stream >> branchPriority;

        long iterativeDeepeningTimeLimitInMilliseconds;
        stream >> iterativeDeepeningTimeLimitInMilliseconds;
        optional<chrono::milliseconds> iterativeDeepeningTimeLimit;
        iterativeDeepeningTimeLimit.reset();
        if (iterativeDeepeningTimeLimitInMilliseconds > 0) {
            iterativeDeepeningTimeLimit = chrono::milliseconds(iterativeDeepeningTimeLimitInMilliseconds);
        }

        long long fullHorizonBabNodesCountLimit;
        stream >> fullHorizonBabNodesCountLimit;

        return SpecializedSolverConfig(
                usePrimalHeuristicBlockDetection != 0,
                usePrimalHeuristicPackToBlocksByCp != 0,
                primalHeuristicPackToBlocksByCpAllJobs != 0,
                useIterativeDeepening != 0,
                (PrimalHeuristicBlockFinding)blockFinding,
                (PrimalHeuristicBlockFindingStrategy)blockFindingStrategy,
                (JobsJoiningOnGcd)jobsJoiningOnGcd,
                (BranchPriority)branchPriority,
                iterativeDeepeningTimeLimit,
                fullHorizonBabNodesCountLimit < 0 ? optional<long long>() : optional<long long>(fullHorizonBabNodesCountLimit));
    }

}

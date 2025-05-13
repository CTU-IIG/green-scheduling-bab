// This file is released under MIT license.
// See file LICENSE.txt for more information.

#include <iostream>
#include <fstream>
#include <set>
#include <omp.h>
#include "GeneticAlgorithm.h"
#include "../input/readers/CppInputReader.h"

using namespace std;
using namespace escs;

int main(int /*argc*/, char **argv) {
    cout << "In cpp" << endl;

    auto solverConfigPath = string(argv[1]);
    auto specializedSolverConfigPath = string(argv[2]);

    auto instancePath = string(argv[3]);
    auto resultPath = string(argv[4]);

    auto solverConfig = SolverConfig::ReadFromPath(solverConfigPath);
    auto specializedSolverConfig = GeneticAlgorithm::SpecializedSolverConfig::ReadFromPath(
            specializedSolverConfigPath);

    CppInputReader inputReader;
    auto instance = inputReader.readFromPath(instancePath);

    GeneticAlgorithm solver(instance, solverConfig, specializedSolverConfig);
    solver.solve();

    auto result = solver.GetResult();
    result.writeToPath(resultPath);

    return 0;

}

namespace escs {
    GeneticAlgorithm::GeneticAlgorithm(
            const Instance &instance,
            SolverConfig &solverConfig,
            const SpecializedSolverConfig &specializedSolverConfig) :
                mInstance(instance),
                mSolverConfig(solverConfig),
                mSpecializedSolverConfig(specializedSolverConfig) {

        mFixedPermCostComputation.reset(new FixedPermCostComputation(
                mInstance.getTotalProcTime(),
                mInstance.mIntervals.size(),
                mInstance.mEarliestOnIntervalIdx,
                mInstance.mLatestOnIntervalIdx,
                mInstance.mOnPowerConsumption,
                mInstance.mFullOptimalSwitchingCosts,
                mInstance.mCumulativeEnergyCost,
                vector<bool>(mInstance.mIntervals.size(), true)));
    }

    Status GeneticAlgorithm::solve() {
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

    void GeneticAlgorithm::solveInternal() {
        mStatus = Status::NoSolution;
        mProcTimesPerm = vector<int>();
        mStartTimesPerm = vector<int>();
        mObj = optional<int>();

        if (mSolverConfig.mNumWorkers >= 1) {
            // Currently, only FixedPermCostComputation runs in parallel and uses OpenMP.
            omp_set_num_threads(mSolverConfig.mNumWorkers);
        }

        GA_Type ga_obj(mSolverConfig.mRandom, mSolverConfig.mTimeLimit);
        ga_obj.problem_mode= EA::GA_MODE::SOGA;
        ga_obj.multi_threading = false;
        ga_obj.verbose = false;
        ga_obj.population = mSpecializedSolverConfig.mPopulationSize;
        ga_obj.generation_max = mSpecializedSolverConfig.mGenerationsCount;
        ga_obj.calculate_SO_total_fitness = [&](auto &X) {
            return CalculateFitness(X);
        };
        ga_obj.init_genes = [&](auto &solution, auto &rnd01) {
            InitGenes(solution, rnd01);
        };
        ga_obj.eval_solution = [&](auto &solution, auto &cost) {
            return EvalSolution(solution, cost);
        };
        ga_obj.mutate = [&](auto &baseSolution, auto &rnd01, auto shrinkScale) {
            auto mutationStrategy = this->mSpecializedSolverConfig.mMutationStrategy;

            if (mutationStrategy == SelectStrategyRandomly) {
                if (rnd01() <= 0.5) {
                    mutationStrategy = Swap;
                }
                else {
                    mutationStrategy = BlockInsertion;
                }
            }

            switch (mutationStrategy) {
                case Swap:
                    return MutateSwap(baseSolution, rnd01, shrinkScale);
                case BlockInsertion:
                    return MutateBlockInsertion(baseSolution, rnd01, shrinkScale);
                case SwapDiffProcTimes:
                    return MutateSwapDiffProcTimes(baseSolution, rnd01, shrinkScale);
                default:
                    throw logic_error("No mutation strategy specified.");
            }
        };
        ga_obj.crossover = [&](auto &parent1, auto &parent2, auto &rnd01) {
            switch (this->mSpecializedSolverConfig.mCrossoverStrategy) {
                case Sequential:
                    return CrossoverSequential(parent1, parent2, rnd01);
                case TwoPoint:
                    return CrossoverTwoPoint(parent1, parent2, rnd01);
                default:
                    throw logic_error("No crossover strategy specified.");
            }
        };
        ga_obj.SO_report_generation= [&](auto generationNumber, auto &lastGeneration, auto &bestSolution) {
            ReportGeneration(generationNumber, lastGeneration, bestSolution);
        };
        ga_obj.best_stall_max = mSpecializedSolverConfig.mBestStallMax;
        ga_obj.average_stall_max = mSpecializedSolverConfig.mAverageStallMax;
        ga_obj.elite_count = mSpecializedSolverConfig.mEliteCount;
        ga_obj.crossover_fraction = mSpecializedSolverConfig.mCrossoverFraction;
        ga_obj.mutation_rate = mSpecializedSolverConfig.mMutationRate;
        ga_obj.solve();

        if (mObj.has_value()) {
            mStartTimesPerm = ComputeObjective(mProcTimesPerm).second;
        }
    }

    vector<int> GeneticAlgorithm::GetStartTimes() const {
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

    Result GeneticAlgorithm::GetResult() const {
        return Result(
                mStatus,
                mStopwatch.timeLimitReached(mSolverConfig.mTimeLimit),
                mObj,
                GetStartTimes());
    }

    pair<optional<int>, vector<int>> GeneticAlgorithm::ComputeObjective(const vector<int> &procTimes) {
        // TODO: gcd? probably at the beginning?

        for (int position = 0; position < (int)procTimes.size(); position++) {
            mFixedPermCostComputation->join(position, procTimes[position]);
        }

        int cost = mFixedPermCostComputation->recomputeCost();
        auto result = make_pair(optional<int>(), vector<int>());
        if (cost != Instance::NO_VALUE) {
            result = make_pair(optional<int>(cost), mFixedPermCostComputation->reconstructStartTimes());
        }

        mFixedPermCostComputation->setProcTimes(0, 1);

        return result;
    }

    void GeneticAlgorithm::InitGenes(GeneticAlgorithmSolution& solution,const std::function<double(void)> &rnd01) {
        mt19937 random((unsigned long)(rnd01() * 10000000));
        for (auto &pJob : mInstance.mJobs) {
            solution.mProcessingTimes.push_back(pJob->mProcessingTime);
        }

        shuffle(solution.mProcessingTimes.begin(), solution.mProcessingTimes.end(), random);
    }

    bool GeneticAlgorithm::EvalSolution(
            const GeneticAlgorithmSolution& solution,
            GeneticAlgorithmCost &cost) {
        auto fixedPermCost = ComputeObjective(solution.mProcessingTimes).first;
        if (fixedPermCost.has_value()) {
            cost.mCost = fixedPermCost.value();
            return true;
        }
        else {
            return false;
        }
    }

    int GeneticAlgorithm::NextRandomInt(int minValue, int maxValue, double randValue)
    {
        int diff = (maxValue - minValue) + 1;
        int step = (int)(randValue * diff);
        return min(maxValue, step + minValue);
    }

    GeneticAlgorithmSolution GeneticAlgorithm::MutateSwap(
            const GeneticAlgorithmSolution& baseSolution,
            const std::function<double(void)> &rnd01,
            double shrinkScale)
    {
        GeneticAlgorithmSolution newSolution;
        newSolution.mProcessingTimes = baseSolution.mProcessingTimes;
        int changesCount = (int)floor(shrinkScale * newSolution.mProcessingTimes.size());

        for (int i = 0; i < changesCount; i++) {
            int srcPos = min((int)(rnd01() * newSolution.mProcessingTimes.size()), (int)newSolution.mProcessingTimes.size() - 1);
            int destPos = min((int)(rnd01() * newSolution.mProcessingTimes.size()), (int)newSolution.mProcessingTimes.size() - 1);
            swap(newSolution.mProcessingTimes[srcPos], newSolution.mProcessingTimes[destPos]);
        }

        return newSolution;
    }

    GeneticAlgorithmSolution GeneticAlgorithm::MutateSwapDiffProcTimes(
            const GeneticAlgorithmSolution& baseSolution,
            const std::function<double(void)> &rnd01,
            double shrinkScale)
    {
        GeneticAlgorithmSolution newSolution;
        newSolution.mProcessingTimes = baseSolution.mProcessingTimes;
        int changesCount = (int)floor(shrinkScale * newSolution.mProcessingTimes.size());

        for (int i = 0; i < changesCount; i++) {
            int srcPos = min((int)(rnd01() * newSolution.mProcessingTimes.size()), (int)newSolution.mProcessingTimes.size() - 1);
            int destPos = min((int)(rnd01() * newSolution.mProcessingTimes.size()), (int)newSolution.mProcessingTimes.size() - 1);
            if (newSolution.mProcessingTimes[srcPos] == newSolution.mProcessingTimes[destPos]) {
                // TODO: slow!!!
                i--;
                continue;
            }
            swap(newSolution.mProcessingTimes[srcPos], newSolution.mProcessingTimes[destPos]);
        }

        return newSolution;
    }

    GeneticAlgorithmSolution GeneticAlgorithm::MutateBlockInsertion(
            const GeneticAlgorithmSolution& baseSolution,
            const std::function<double(void)> &rnd01,
            double shrinkScale) {
        GeneticAlgorithmSolution newSolution;
        newSolution.mProcessingTimes = baseSolution.mProcessingTimes;

        int insertionBlockSize = min(
                max(1, (int)floor(shrinkScale * newSolution.mProcessingTimes.size())),
                (int)newSolution.mProcessingTimes.size());

        int fromPos = NextRandomInt(0, newSolution.mProcessingTimes.size() - insertionBlockSize, rnd01());
        vector<int> block = vector<int>(
                newSolution.mProcessingTimes.begin() + fromPos,
                newSolution.mProcessingTimes.begin() + fromPos + insertionBlockSize);
        newSolution.mProcessingTimes.erase(
                newSolution.mProcessingTimes.begin() + fromPos,
                newSolution.mProcessingTimes.begin() + fromPos + insertionBlockSize);
        int toPos = NextRandomInt(0, newSolution.mProcessingTimes.size(), rnd01());
        newSolution.mProcessingTimes.insert(
                newSolution.mProcessingTimes.begin() + toPos,
                block.begin(),
                block.end());

        return newSolution;
    }

    GeneticAlgorithmSolution GeneticAlgorithm::CrossoverSequential(
            const GeneticAlgorithmSolution& parent1,
            const GeneticAlgorithmSolution& parent2,
            const std::function<double(void)> &rnd01) {
        GeneticAlgorithmSolution child;

        map<int, int> remainingProcTimeCounts;
        for (auto *pJob : mInstance.mJobs) {
            auto it = remainingProcTimeCounts.find(pJob->mProcessingTime);
            if (it == remainingProcTimeCounts.end()) {
                remainingProcTimeCounts[pJob->mProcessingTime] = 1;
            }
            else {
                it->second++;
            }
        }

        int parent1Pointer = 0;
        int parent2Pointer = 0;

        while (child.mProcessingTimes.size() < parent1.mProcessingTimes.size()) {
            bool fromParent1 = rnd01() <= 0.5;
            int &inheritPointer = fromParent1 ? parent1Pointer : parent2Pointer;
            const GeneticAlgorithmSolution &inheritParent = fromParent1 ? parent1 : parent2;

            // Find the next unused processing time from inherited parent (will be pointed by pointer).
            while (inheritPointer < (int)inheritParent.mProcessingTimes.size() &&
                   remainingProcTimeCounts[inheritParent.mProcessingTimes[inheritPointer]] == 0) {
                inheritPointer++;
            }

            if (inheritPointer < (int)inheritParent.mProcessingTimes.size()) {
                int procTime = inheritParent.mProcessingTimes[inheritPointer];
                int procTimeCount = remainingProcTimeCounts[procTime];
                remainingProcTimeCounts[procTime] = procTimeCount - 1;
                child.mProcessingTimes.push_back(procTime);
            }
            else {
                // No processing time found, put the remaining other parent.
                int &otherInheritPointer = fromParent1 ? parent2Pointer : parent1Pointer;
                const GeneticAlgorithmSolution &otherInheritParent = fromParent1 ? parent2 : parent1;
                while (otherInheritPointer < (int)otherInheritParent.mProcessingTimes.size()) {
                    if (remainingProcTimeCounts[otherInheritParent.mProcessingTimes[otherInheritPointer]] > 0) {
                        int procTime = otherInheritParent.mProcessingTimes[otherInheritPointer];
                        int procTimeCount = remainingProcTimeCounts[procTime];
                        remainingProcTimeCounts[procTime] = procTimeCount - 1;
                        child.mProcessingTimes.push_back(procTime);
                    }
                    otherInheritPointer++;
                }
            }
        }

        return child;
    }

    GeneticAlgorithmSolution GeneticAlgorithm::CrossoverTwoPoint(
            const GeneticAlgorithmSolution& parent1,
            const GeneticAlgorithmSolution& parent2,
            const std::function<double(void)> &rnd01) {
        GeneticAlgorithmSolution child;
        child.mProcessingTimes = vector<int>(parent1.mProcessingTimes.size(), -1);

        map<int, int> remainingProcTimeCounts;
        for (auto *pJob : mInstance.mJobs) {
            auto it = remainingProcTimeCounts.find(pJob->mProcessingTime);
            if (it == remainingProcTimeCounts.end()) {
                remainingProcTimeCounts[pJob->mProcessingTime] = 1;
            }
            else {
                it->second++;
            }
        }

        int swathStartIndex = min((int)(rnd01() * parent1.mProcessingTimes.size()), (int)parent1.mProcessingTimes.size() - 1);
        int swathEndIndex = min((int)(rnd01() * parent1.mProcessingTimes.size()), (int)parent1.mProcessingTimes.size() - 1);
        if (swathStartIndex > swathEndIndex)
        {
            std::swap(swathStartIndex, swathEndIndex);
        }

        bool fromParent1 = rnd01() <= 0.5;
        const GeneticAlgorithmSolution &swathParent = fromParent1 ? parent1 : parent2;
        const GeneticAlgorithmSolution &otherParent = fromParent1 ? parent2 : parent1;

        for (int swathIndex = swathStartIndex; swathIndex <= swathEndIndex; swathIndex++)
        {
            auto procTime = swathParent.mProcessingTimes[swathIndex];
            child.mProcessingTimes[swathIndex] = procTime;
            int procTimeCount = remainingProcTimeCounts[procTime];
            remainingProcTimeCounts[procTime] = procTimeCount - 1;
        }

        auto nextChildIndex = 0;
        if (swathStartIndex == 0) {
            nextChildIndex = swathEndIndex + 1;
        }
        for (int index = 0; index < (int)otherParent.mProcessingTimes.size(); index++) {
            auto procTime = otherParent.mProcessingTimes[index];
            int procTimeCount = remainingProcTimeCounts[procTime];
            if (procTimeCount > 0) {
                child.mProcessingTimes[nextChildIndex] = procTime;
                remainingProcTimeCounts[procTime] = procTimeCount - 1;

                nextChildIndex++;
                if (nextChildIndex == swathStartIndex) {
                    nextChildIndex = swathEndIndex + 1;
                }
            }
        }

        return child;
    }

    double GeneticAlgorithm::CalculateFitness(const GA_Type::thisChromosomeType &X) {
        return (double)X.middle_costs.mCost;
    }

    void GeneticAlgorithm::ReportGeneration(
            int generationNumber,
            const EA::GenerationType<GeneticAlgorithmSolution,GeneticAlgorithmCost> &lastGeneration,
            const GeneticAlgorithmSolution& bestSolution) {
        if (bestSolution.mProcessingTimes.size() > 0) {
            mProcTimesPerm = bestSolution.mProcessingTimes;
            mObj = (unsigned long)round(lastGeneration.best_total_cost);
        }

        std::cout
                <<"Generation ["<<generationNumber<<"], "
                <<"Best="<<(unsigned long)round(lastGeneration.best_total_cost)<<", "
                <<"Average="<<lastGeneration.average_cost<<", "
                <<"Best genes=("<<bestSolution.to_string()<<")"<<", "
                <<"Exe_time="<<lastGeneration.exe_time
                <<std::endl;
    }

    GeneticAlgorithm::SpecializedSolverConfig::SpecializedSolverConfig(
            int generationsCount,
            int populationSize,
            int eliteCount,
            double crossoverFraction,
            CrossoverStrategy crossoverStrategy,
            MutationStrategy mutationStrategy,
            double mutationRate,
            int bestStallMax,
            int averageStallMax) :
            mGenerationsCount(generationsCount),
            mPopulationSize(populationSize),
            mEliteCount(eliteCount),
                mCrossoverFraction(crossoverFraction),
                mCrossoverStrategy(crossoverStrategy),
                mMutationStrategy(mutationStrategy),
                mMutationRate(mutationRate),
                mBestStallMax(bestStallMax),
                mAverageStallMax(averageStallMax) {

    }

    GeneticAlgorithm::SpecializedSolverConfig GeneticAlgorithm::SpecializedSolverConfig::ReadFromPath(string specializedSolverConfigPath) {
        ifstream stream;
        stream.open(specializedSolverConfigPath);

        int generationsCount;
        stream >> generationsCount;

        int populationSize;
        stream >> populationSize;

        int eliteCount;
        stream >> eliteCount;

        double crossoverFraction;
        stream >> crossoverFraction;

        int crossoverStrategy;
        stream >> crossoverStrategy;

        int mutationStrategy;
        stream >> mutationStrategy;

        double mutationRate;
        stream >> mutationRate;
        
        int bestStallMax;
        stream >> bestStallMax;
        
        int averageStallMax;
        stream >> averageStallMax;

        return SpecializedSolverConfig(
                generationsCount,
                populationSize,
                eliteCount,
                crossoverFraction,
                (CrossoverStrategy)crossoverStrategy,
                (MutationStrategy)mutationStrategy,
                mutationRate,
                bestStallMax,
                averageStallMax);
    }
}
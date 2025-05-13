// This file is released under MIT license.
// See file LICENSE.txt for more information.

#ifndef ENERGYSTATESANDCOSTSSCHEDULING_GENETICALGORITHM_H
#define ENERGYSTATESANDCOSTSSCHEDULING_GENETICALGORITHM_H

#include <algorithm>
#include <map>

#include "../openga/openGA.hpp"
#include "../input/Instance.h"
#include "../datastructs/FixedPermCostComputation.h"
#include "SolverConfig.h"
#include "../output/Result.h"

using namespace std;

namespace escs {

    struct GeneticAlgorithmSolution
    {
        vector<int> mProcessingTimes;

        std::string to_string() const
        {
            string str = "";
            for (auto processingTime : mProcessingTimes)
            {
                if (str.length() > 0) {
                    str += ", ";
                }

                str += std::to_string(processingTime);
            }

            return str;
        }
    };

    struct GeneticAlgorithmCost
    {
        int mCost;
    };

    typedef EA::Genetic<GeneticAlgorithmSolution, GeneticAlgorithmCost> GA_Type;
    typedef EA::GenerationType<GeneticAlgorithmSolution, GeneticAlgorithmCost> Generation_Type;

    class GeneticAlgorithm {
    public:
        enum CrossoverStrategy
        {
            Sequential = 0,
            TwoPoint = 1
        };

        enum MutationStrategy
        {
            Swap = 0,
            BlockInsertion = 1,
            SelectStrategyRandomly = 2,
            SwapDiffProcTimes = 3,
        };

        class SpecializedSolverConfig {
        public:
            const int mGenerationsCount;
            const int mPopulationSize;
            const int mEliteCount;
            const double mCrossoverFraction;
            const CrossoverStrategy mCrossoverStrategy;
            const MutationStrategy mMutationStrategy;
            const double mMutationRate;
            const int mBestStallMax;
            const int mAverageStallMax;

            SpecializedSolverConfig(
                    int generationsCount,
                    int populationSize,
                    int eliteCount,
                    double crossoverFraction,
                    CrossoverStrategy crossoverStrategy,
                    MutationStrategy mutationStrategy,
                    double mutationRate,
                    int bestStallMax,
                    int averageStallMax);

            static SpecializedSolverConfig ReadFromPath(string specializedSolverConfigPath);
        };

    private:
        const Instance &mInstance;
        SolverConfig &mSolverConfig;
        const SpecializedSolverConfig &mSpecializedSolverConfig;

        Stopwatch mStopwatch;

        Status mStatus;
        vector<int> mProcTimesPerm;
        vector<int> mStartTimesPerm;
        optional<int> mObj;

        unique_ptr<FixedPermCostComputation> mFixedPermCostComputation;

    public:
        GeneticAlgorithm(
                const Instance &instance,
                SolverConfig &solverConfig,
                const SpecializedSolverConfig &specializedSolverConfig);

        Status solve();
        void solveInternal();

        vector<int> GetStartTimes() const;
        Result GetResult() const;

        int NextRandomInt(int minValue, int maxValue, double randValue);

        pair<optional<int>, vector<int>> ComputeObjective(const vector<int> &procTimes);

        void InitGenes(GeneticAlgorithmSolution& solution,const std::function<double(void)> &rnd01);

        bool EvalSolution(const GeneticAlgorithmSolution& solution, GeneticAlgorithmCost &cost);

        GeneticAlgorithmSolution MutateSwap(
                const GeneticAlgorithmSolution& baseSolution,
                const std::function<double(void)> &rnd01,
                double shrinkScale);

        GeneticAlgorithmSolution MutateSwapDiffProcTimes(
                const GeneticAlgorithmSolution& baseSolution,
                const std::function<double(void)> &rnd01,
                double shrinkScale);

        GeneticAlgorithmSolution MutateBlockInsertion(
                const GeneticAlgorithmSolution& baseSolution,
                const std::function<double(void)> &rnd01,
                double shrinkScale);

        GeneticAlgorithmSolution CrossoverSequential(
                const GeneticAlgorithmSolution& parent1,
                const GeneticAlgorithmSolution& parent2,
                const std::function<double(void)> &rnd01);

        GeneticAlgorithmSolution CrossoverTwoPoint(
                const GeneticAlgorithmSolution& parent1,
                const GeneticAlgorithmSolution& parent2,
                const std::function<double(void)> &rnd01);

        double CalculateFitness(const GA_Type::thisChromosomeType &X);

        void ReportGeneration(
                int generationNumber,
                const EA::GenerationType<GeneticAlgorithmSolution,GeneticAlgorithmCost> &lastGeneration,
                const GeneticAlgorithmSolution& bestSolution);
    };

}

#endif //ENERGYSTATESANDCOSTSSCHEDULING_GENETICALGORITHM_H

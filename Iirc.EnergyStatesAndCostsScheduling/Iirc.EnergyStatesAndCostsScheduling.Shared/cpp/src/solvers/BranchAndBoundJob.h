#include <memory>
// This file is released under MIT license.
// See file LICENSE.txt for more information.

#ifndef ENERGYSTATESANDCOSTSSCHEDULING_BRANCHANDBOUNDONJOB_H
#define ENERGYSTATESANDCOSTSSCHEDULING_BRANCHANDBOUNDONJOB_H

#include <functional>
#include "../input/Instance.h"
#include "SolverConfig.h"
#include "../utils/Stopwatch.h"
#include "../datastructs/FixedPermCostComputation.h"
#include "../datastructs/GcdOfValues.h"
#include "../output/Status.h"
#include "../output/Result.h"
#include "../datastructs/Block.h"
#include <gurobi_c++.h>


using namespace std;

namespace escs {

    class BranchAndBoundOnJob
    {
    public:
        enum JobsJoiningOnGcd
        {
            Off = 0,
            ROOT = 1,
            WHOLE_TREE = 2
        };

        enum PrimalHeuristicBlockFinding
        {
            BF_OFF = 0,
            BF_ROOT = 1,
            BF_WHOLE_TREE = 2
        };

        enum PrimalHeuristicBlockFindingStrategy
        {
            MinimizeLengthDifference = 0
        };

        enum BranchPriority
        {
            Random = 0,
            ForcedSpace = 1,
            JoinToPrev = 2,
            DynamicByBlockFitting = 3
        };

        class SpecializedSolverConfig {
        public:
            const bool mUsePrimalHeuristicBlockDetection;
            const bool mUsePrimalHeuristicPackToBlocksByCp;
            const bool mPrimalHeuristicPackToBlocksByCpAllJobs;
            const bool mUseIterativeDeepening;
            const PrimalHeuristicBlockFinding mBlockFinding;
            const PrimalHeuristicBlockFindingStrategy mBlockFindingStrategy;
            const JobsJoiningOnGcd mJobsJoiningOnGcd;
            const BranchPriority mBranchPriority;
            const optional<chrono::milliseconds> mIterativeDeepeningTimeLimit;
            const optional<long long> mFullHorizonBabNodesCountLimit;


            SpecializedSolverConfig(
                    bool usePrimalHeuristicBlockDetection,
                    bool usePrimalHeuristicPackToBlocksByCp,
                    bool primalHeuristicPackToBlocksByCpAllJobs,
                    bool useIterativeDeepening,
                    PrimalHeuristicBlockFinding blockFinding,
                    PrimalHeuristicBlockFindingStrategy blockFindingStrategy,
                    JobsJoiningOnGcd jobsJoiningOnGcd,
                    BranchPriority branchPriority,
                    optional<chrono::milliseconds> iterativeDeepeningTimeLimit,
                    optional<long long> fullHorizonBabNodesCountLimit);

            static SpecializedSolverConfig ReadFromPath(string specializedSolverConfigPath);
        };

    private:
        void solveInternal();
        void enterNode(
                vector<vector<int>> &fixedProcTimesBlocks,
                int fixedProcTimesCount,
                map<int, int> &remainingProcTimeCounts,
                int remainingProcTime,
                FixedPermCostComputation &fixedPermCostComputation,
                GcdOfValues &gcdOfValues,
                int currJoinedGcd,
                optional<int> inheritedLowerBound,
                vector<Block> remProcBlocksReversed,
                bool joinToPrevBlock);

        vector<int> PerformPrimalHeuristicBlockFinding(const vector<Block> &relaxedBlocks, bool &sameAsRelaxedBlocks);

        bool PerformPrimalHeuristicBlockDetection(
                vector<vector<int>> &fixedProcTimesBlocks,
                map<int, int> &remainingProcTimeCounts,
                int remainingProcTime,
                FixedPermCostComputation &fixedPermCostComputation);

        bool PerformPrimalHeuristicPackToBlocksByCp(
                vector<vector<int>> &fixedProcTimesBlocks,
                map<int, int> &remainingProcTimeCounts,
                int remainingProcTime,
                FixedPermCostComputation &fixedPermCostComputation);

        void printCurrNodeLogPrefix(const vector<vector<int>> &fixedProcTimesBlocks) const {
            string nodeId;
            int fixedProcTimesCount = 0;
            for (int i = 0; i < (int)fixedProcTimesBlocks.size(); i++) {
                if (i > 0) {
                    nodeId += "_ ";
                }
                auto &procTimesBlock = fixedProcTimesBlocks[i];
                for (int p : procTimesBlock) {
                    nodeId += to_string(p);
                    nodeId += " ";
                    fixedProcTimesCount++;
                }
            }
            cout << left << setw(15) << nodeId << " | ";
            for (int i = 0; i < fixedProcTimesCount; i++) {
                (void)i;
                cout << "-- ";
            }
        }

        vector<int> flatten(const vector<vector<int>> &xss) const {
            vector<int> result;
            for (auto &xs : xss) {
                for (auto x : xs) {
                    result.push_back(x);
                }
            }

            return result;
        }

        vector<int> startTimesFromBlockProcTimes(
                const vector<int> &blockStartTimes,
                const vector<vector<int>> &procTimeBlocks) const {
            vector<int> startTimes;
            int nextStartTime = 0;
            for (int i = 0; i < (int)procTimeBlocks.size(); i++) {
                nextStartTime = blockStartTimes[i];
                for (int procTime : procTimeBlocks[i]) {
                    startTimes.push_back(nextStartTime);
                    nextStartTime += procTime;
                }
            }

            return startTimes;
        }

    protected:
        const Instance &mInstance;
        SolverConfig &mSolverConfig;
        const SpecializedSolverConfig &mSpecializedSolverConfig;
        Stopwatch mStopwatch;
        Stopwatch mPrimalHeuristicBlockFindingStopwatch;
        Stopwatch mPrimalHeuristicBlockDetectionStopwatch;
        Stopwatch mPrimalHeuristicPackToBlocksByCpStopwatch;
        const GRBEnv mEnv;

        Status mStatus;
        optional<int> mCurrBestObj;
        vector<int> mCurrBestPermProcTimes;
        vector<int> mCurrBestPermStartTimes;

        chrono::milliseconds mLowerBoundTotalDuration;
        chrono::milliseconds mPrimalHeuristicBlockFindingTotalDuration;
        chrono::milliseconds mPrimalHeuristicBlockDetectionTotalDuration;
        chrono::milliseconds mPrimalHeuristicPackToBlocksByCpTotalDuration;

        uniform_int_distribution<> mRandomBranchPriorityDist;

        std::unique_ptr<FixedPermCostComputation> mFixedBlocksComputation;

        // Statistics.
        long long mNodesCount;
        long long mCurrNode;
        long long mPrimalHeuristicBlockDetectionFoundSolution;
        long long mUsePrimalHeuristicPackToBlocksByCpFoundSolution;
        long long mJobsJoinedOnLargerGcd;
        int mRootLowerBound;

        bool mNodesCountLimitReached;

        bool stopSearching() const {
            return mNodesCountLimitReached || mStopwatch.timeLimitReached(mSolverConfig.mTimeLimit);
        }

    public:
        BranchAndBoundOnJob(
                const Instance &instance,
                SolverConfig &solverConfig,
                const SpecializedSolverConfig &specializedSolverConfig);
        Status solve();

        vector<int> getStartTimes() const;
        Result getResult() const;

    };

    Result iterativeDeeping(
            SolverConfig &solverConfig,
            const BranchAndBoundOnJob::SpecializedSolverConfig &specializedSolverConfig,
            const Instance &instance);

    vector<bool> puffBlocksToProcessableIntervals(
            const Instance &instance,
            const vector<Block> &blocks,
            int puffSize);
}


#endif //ENERGYSTATESANDCOSTSSCHEDULING_BRANCHANDBOUNDONJOB_H

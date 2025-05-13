// This file is released under MIT license.
// See file LICENSE.txt for more information.

#ifndef ENERGYSTATESANDCOSTSSCHEDULING_RESULT_H
#define ENERGYSTATESANDCOSTSSCHEDULING_RESULT_H

#include <optional>
#include <map>
#include <chrono>
#include "../input/Instance.h"
#include "Status.h"

using namespace std;

namespace escs {
    class Result {
    public:
        const Status mStatus;
        const optional<int> mObjective;
        const bool mTimeLimitReached;
        const vector<int> mStartTimes;
        const optional<long long> mNodesCount;
        const optional<long long> mPrimalHeuristicBlockDetectionFoundSolution;
        const optional<long long> mPrimalHeuristicPackToBlocksByCpFoundSolution;
        const optional<long long> mJobsJoinedOnLargerGcd;
        const optional<int> mRootLowerBound;
        const optional<chrono::milliseconds> mLowerBoundTotalDuration;
        const optional<chrono::milliseconds> mPrimalHeuristicBlockDetectionTotalDuration;
        const optional<chrono::milliseconds> mPrimalHeuristicPackToBlockByCpTotalDuration;
        const optional<chrono::milliseconds> mPrimalHeuristicBlockFindingTotalDuration;

        Result(
                Status status,
                bool timeLimitReached,
                optional<int> objective = optional<int>(),
                vector<int> startTimes = vector<int>(),
                optional<long long> nodesCount = optional<long long>(),
                optional<long long> mPrimalHeuristicBlockDetectionFoundSolution = optional<long long>(),
                optional<long long> mPrimalHeuristicPackToBlocksByCpFoundSolution = optional<long long>(),
                optional<long long> mJobsJoinedOnLargerGcd = optional<long long>(),
                optional<int> mRootLowerBound = optional<int>(),
                optional<chrono::milliseconds> lowerBoundTotalDuration = optional<chrono::milliseconds>(),
                optional<chrono::milliseconds> primalHeuristicBlockDetectionTotalDuration = optional<chrono::milliseconds>(),
                optional<chrono::milliseconds> primalHeuristicPackToBlockByCpTotalDuration = optional<chrono::milliseconds>(),
                optional<chrono::milliseconds> primalHeuristicBlockFindingTotalDuration = optional<chrono::milliseconds>());

        void writeToPath(string resultPath);
    };
}


#endif //ENERGYSTATESANDCOSTSSCHEDULING_RESULT_H

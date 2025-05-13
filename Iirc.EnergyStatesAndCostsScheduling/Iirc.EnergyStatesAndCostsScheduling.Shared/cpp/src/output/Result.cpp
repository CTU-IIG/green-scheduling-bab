// This file is released under MIT license.
// See file LICENSE.txt for more information.

#include <fstream>
#include "Result.h"

namespace escs {
    Result::Result(
            Status status,
            bool timeLimitReached,
            optional<int> objective,
            vector<int> startTimes,
            optional<long long> nodesCount,
            optional<long long> primalHeuristicBlockDetectionFoundSolution,
            optional<long long> primalHeuristicPackToBlocksByCpFoundSolution,
            optional<long long> jobsJoinedOnLargerGcd,
            optional<int> rootLowerBound,
            optional<chrono::milliseconds> lowerBoundTotalDuration,
            optional<chrono::milliseconds> primalHeuristicBlockDetectionTotalDuration,
            optional<chrono::milliseconds> primalHeuristicPackToBlockByCpTotalDuration,
            optional<chrono::milliseconds> primalHeuristicBlockFindingTotalDuration)
        : mStatus(status),
                  mObjective(objective),
                  mTimeLimitReached(timeLimitReached),
                  mStartTimes(move(startTimes)),
                  mNodesCount(nodesCount),
                  mPrimalHeuristicBlockDetectionFoundSolution(primalHeuristicBlockDetectionFoundSolution),
                  mPrimalHeuristicPackToBlocksByCpFoundSolution(primalHeuristicPackToBlocksByCpFoundSolution),
                  mJobsJoinedOnLargerGcd(jobsJoinedOnLargerGcd),
                  mRootLowerBound(rootLowerBound),
                  mLowerBoundTotalDuration(lowerBoundTotalDuration),
                  mPrimalHeuristicBlockDetectionTotalDuration(primalHeuristicBlockDetectionTotalDuration),
                  mPrimalHeuristicPackToBlockByCpTotalDuration(primalHeuristicPackToBlockByCpTotalDuration),
                  mPrimalHeuristicBlockFindingTotalDuration(primalHeuristicBlockFindingTotalDuration)
            {

            }

    void Result::writeToPath(string resultPath) {
        ofstream stream;
        stream.open(resultPath,  ofstream::out);

        // Status.
        switch (mStatus) {
            case Status::NoSolution:
                stream << "NoSolution" << endl;
                break;

            case Status::Heuristic:
                stream << "Heuristic" << endl;
                break;

            case Status::Infeasible:
                stream << "Infeasible" << endl;
                break;

            case Status::Optimal:
                stream << "Optimal" << endl;
                break;
        }

        // Objective.
        if (mObjective.has_value()) {
            stream << mObjective.value() << endl;
        }
        else {
            stream << -1 << endl;
        }

        // Time limit reached.
        stream << mTimeLimitReached << endl;

        // Start times.
        if (mStartTimes.empty()) {
            stream << "NoSolution" << endl;
        } else {
            for (int jobIdx = 0; jobIdx < (int)mStartTimes.size(); jobIdx++) {
                stream << jobIdx << " " << mStartTimes[jobIdx] << " ";
            }
            stream << endl;
        }

        // Nodes count.
        if (mNodesCount.has_value()) {
            stream << mNodesCount.value() << endl;
        }
        else {
            stream << -1 << endl;
        }

        if (mPrimalHeuristicBlockDetectionFoundSolution.has_value()) {
            stream << mPrimalHeuristicBlockDetectionFoundSolution.value() << endl;
        }
        else {
            stream << -1 << endl;
        }

        if (mPrimalHeuristicPackToBlocksByCpFoundSolution.has_value()) {
            stream << mPrimalHeuristicPackToBlocksByCpFoundSolution.value() << endl;
        }
        else {
            stream << -1 << endl;
        }

        if (mJobsJoinedOnLargerGcd.has_value()) {
            stream << mJobsJoinedOnLargerGcd.value() << endl;
        }
        else {
            stream << -1 << endl;
        }

        if (mRootLowerBound.has_value()) {
            stream << mRootLowerBound.value() << endl;
        }
        else {
            stream << -1 << endl;
        }

        if (mLowerBoundTotalDuration.has_value()) {
            stream << mLowerBoundTotalDuration->count() << endl;
        }
        else {
            stream << -1 << endl;
        }

        if (mPrimalHeuristicBlockDetectionTotalDuration.has_value()) {
            stream << mPrimalHeuristicBlockDetectionTotalDuration->count() << endl;
        }
        else {
            stream << -1 << endl;
        }

        if (mPrimalHeuristicPackToBlockByCpTotalDuration.has_value()) {
            stream << mPrimalHeuristicPackToBlockByCpTotalDuration->count() << endl;
        }
        else {
            stream << -1 << endl;
        }

        if (mPrimalHeuristicBlockFindingTotalDuration.has_value()) {
            stream << mPrimalHeuristicBlockFindingTotalDuration->count() << endl;
        }
        else {
            stream << -1 << endl;
        }
    }
}


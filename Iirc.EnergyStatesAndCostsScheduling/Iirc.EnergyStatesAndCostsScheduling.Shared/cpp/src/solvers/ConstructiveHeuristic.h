// This file is released under MIT license.
// See file LICENSE.txt for more information.

#ifndef ENERGYSTATESANDCOSTSSCHEDULING_CONSTRUCTIVEHEURISTIC_H
#define ENERGYSTATESANDCOSTSSCHEDULING_CONSTRUCTIVEHEURISTIC_H

#include <functional>
#include "../input/Instance.h"
#include "SolverConfig.h"
#include "../utils/Stopwatch.h"
#include "../datastructs/FixedPermCostComputation.h"
#include "../datastructs/GcdOfValues.h"
#include "../output/Status.h"
#include "../output/Result.h"

using namespace std;

namespace escs {

    class ConstructiveHeuristic
    {
    public:
        enum Algorithm
        {
            AllPositions = 0,
            RandomPositions = 1,
            AllPositionsWithBlockKeeping = 2
        };

        enum JobsOrdering
        {
            Random = 0,
            ShortestProcessingTimeFirst = 1,
            LongestProcessingTimeFirst = 2,
            AlternateShortestLongestProcessingTime = 3,
            AlternateHalvesShortLongProcessingTime = 4
        };

        class SpecializedSolverConfig {
        public:
            const Algorithm mAlgorithm;
            const JobsOrdering mJobsOrdering;
            const int mRandomPositionsCount;

            SpecializedSolverConfig(
                    Algorithm algorithm,
                    JobsOrdering jobsOrdering,
                    int randomPositionsCount);

            static SpecializedSolverConfig ReadFromPath(string specializedSolverConfigPath);
        };

    private:
        void solveInternal();
        void algorithmAllPositions();
        void algorithmAllPositionsWithBlockKeeping();
        vector<int> getProcessingTimesOrdering();

        pair<optional<int>, vector<int>> computeObjective(
                const Instance &instance,
                vector<int> procTimes);

    protected:
        const Instance &mInstance;
        SolverConfig &mSolverConfig;
        const SpecializedSolverConfig &mSpecializedSolverConfig;
        Stopwatch mStopwatch;

        Status mStatus;
        vector<int> mProcTimesPerm;
        vector<int> mStartTimesPerm;
        optional<int> mObj;

    public:
        ConstructiveHeuristic(
                const Instance &instance,
                SolverConfig &solverConfig,
                const SpecializedSolverConfig &specializedSolverConfig);
        Status solve();

        vector<int> getStartTimes() const;
        Result getResult() const;
    };
}


#endif //ENERGYSTATESANDCOSTSSCHEDULING_CONSTRUCTIVEHEURISTIC_H

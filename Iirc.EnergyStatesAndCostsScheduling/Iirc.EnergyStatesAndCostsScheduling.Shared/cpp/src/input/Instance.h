// This file is released under MIT license.
// See file LICENSE.txt for more information.

#ifndef ENERGYSTATESANDCOSTSSCHEDULING_INSTANCE_H
#define ENERGYSTATESANDCOSTSSCHEDULING_INSTANCE_H

#include <vector>
#include "Job.h"
#include "Interval.h"

using namespace std;

namespace escs {

    class Instance {
    private:
        int mTotalProcTime;

    public:
        const static int NO_VALUE;

        const int mMachinesCount;
        const vector<const Job*> mJobs;
        const vector<const Interval*> mIntervals;
        const int mLengthInterval;
        const int mOnPowerConsumption;
        const int mEarliestOnIntervalIdx;
        const int mLatestOnIntervalIdx;
        const vector<vector<int>> mOptimalSwitchingCosts;
        const vector<vector<int>> mFullOptimalSwitchingCosts;
        const vector<vector<int>> mCumulativeEnergyCost;


        Instance(
                int machinesCount,
                const vector<const Job*> jobs,
                const vector<const Interval*> intervals,
                int lengthInterval,
                int onPowerConsumption,
                int earliestOnIntervalIdx,
                int latestOnIntervalIdx,
                const vector<vector<int>> optimalSwitchingCosts,
                const vector<vector<int>> fullOptimalSwitchingCosts,
                const vector<vector<int>> cumulativeEnergyCost);

        int getTotalProcTime() const {
            return mTotalProcTime;
        }

        ~Instance();
    };
}

#endif //ENERGYSTATESANDCOSTSSCHEDULING_INSTANCE_H

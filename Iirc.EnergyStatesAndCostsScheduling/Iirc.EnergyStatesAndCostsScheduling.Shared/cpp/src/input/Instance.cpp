// This file is released under MIT license.
// See file LICENSE.txt for more information.

#include <limits>
#include "Instance.h"

namespace escs {
    const int Instance::NO_VALUE = numeric_limits<int>::max();

    Instance::Instance(
            int machinesCount,
            const vector<const Job*> jobs,
            const vector<const Interval*> intervals,
            int lengthInterval,
            int onPowerConsumption,
            int earliestOnIntervalIdx,
            int latestOnIntervalIdx,
            const vector<vector<int>> optimalSwitchingCosts,
            const vector<vector<int>> fullOptimalSwitchingCosts,
            const vector<vector<int>> cumulativeEnergyCost)
            : mMachinesCount(machinesCount),
            mJobs(jobs),
            mIntervals(intervals),
            mLengthInterval(lengthInterval),
            mOnPowerConsumption(onPowerConsumption),
            mEarliestOnIntervalIdx(earliestOnIntervalIdx),
            mLatestOnIntervalIdx(latestOnIntervalIdx),
            mOptimalSwitchingCosts(optimalSwitchingCosts),
            mFullOptimalSwitchingCosts(fullOptimalSwitchingCosts),
            mCumulativeEnergyCost(cumulativeEnergyCost)
    {
        mTotalProcTime = 0;
        for (auto pJob : mJobs) {
            mTotalProcTime += pJob->mProcessingTime;
        }
    }

    Instance::~Instance() {
        for (auto job : mJobs)
        {
            delete job;
        }

        for (auto interval : mIntervals)
        {
            delete interval;
        }
    }
}

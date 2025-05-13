// This file is released under MIT license.
// See file LICENSE.txt for more information.

#ifndef ENERGYSTATESANDCOSTSSCHEDULING_FIXEDPERMCOSTCOMPUTATION_H
#define ENERGYSTATESANDCOSTSSCHEDULING_FIXEDPERMCOSTCOMPUTATION_H

#include <vector>
#include "../input/Instance.h"
#include "../utils/Stopwatch.h"

using namespace std;

namespace escs {
    class FixedPermCostComputation {
        // Position: position into permutation.
        // Level: vertical index.
    private:
        const int mTotalProcTime;

        int mOptCost;
        int mLastLevelOptStart;
        vector<vector<int>> mOptPath;
        vector<vector<int>> mCostsOnLevels;
        vector<int> mPermProcTimes;
        vector<int> mPermLevels;
        vector<int> mPermForcedSpaces;
        vector<int> mMaxProcessableIntervals;
        int mCostsValidLevel; // On this level, the costs are valid.
        int mCostsValidPosition; // On this level, the costs are valid.

        const int mNumIntervals;
        const int mEarliestOnIntervalIdx;
        const int mLatestOnIntervalIdx;

        const int mOnPowerConsumption;
        const vector<vector<int>> &mOptSwitchingCosts;
        vector<vector<int>> mOptSwitchingCostsTrans;
        const vector<vector<int>> &mCumulEnergyCost;
        vector<vector<int>> mCumulOnEnergyCostPerProcTime;
        vector<bool> mProcessableIntervals;

        vector<int> mIntervalsTmp;

        Stopwatch mStopwatch;

        int findNextProcessableInterval(const vector<bool> &processableIntervals, int fromIdx);

    public:

        FixedPermCostComputation(
                int totalProcTime,
                int numIntervals,
                int earliestOnIntervalIdx,
                int latestOnIntervalIdx,
                int onPowerConsumption,
                const vector<vector<int>> &optSwitchingCosts,
                const vector<vector<int>> &cumulEnergyCost,
                const vector<bool> &processableIntervals);

        void join(int fromPosition, int positionsCount);
        void split(int fromPosition, int positionsCount);
        void split(int fromPosition, const vector<int> &procTimes);
        void setProcTimes(int fromPosition, int procTime);
        void invalidateCosts(int fromPosition);
        int recomputeCost();
        vector<int> reconstructStartTimes();
        void reset();

        int getOptCost() {
            return this->recomputeCost();
        }

        const vector<int> &getPermProcTimes() const {
            return mPermProcTimes;
        }

        bool hasOptCost() {
            return getOptCost() != Instance::NO_VALUE;
        }

        const chrono::milliseconds getCostComputationTotalDuration() const {
            return mStopwatch.totalDuration();
        }

        void setForcedSpace(int position, int space) {
            mPermForcedSpaces[position] = space;
            this->invalidateCosts(position);
        }
    };
}


#endif //ENERGYSTATESANDCOSTSSCHEDULING_FIXEDPERMCOSTCOMPUTATION_H

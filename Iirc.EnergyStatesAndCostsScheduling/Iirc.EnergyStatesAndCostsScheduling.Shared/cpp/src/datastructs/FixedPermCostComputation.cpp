// This file is released under MIT license.
// See file LICENSE.txt for more information.

#include <algorithm>
#include <numeric>
#include <stdexcept>
#include <cassert>
#include "FixedPermCostComputation.h"
#include "../input/Instance.h"

namespace escs {

    FixedPermCostComputation::FixedPermCostComputation(
            int totalProcTime,
            int numIntervals,
            int earliestOnIntervalIdx,
            int latestOnIntervalIdx,
            int onPowerConsumption,
            const vector<vector<int>> &optSwitchingCosts,
            const vector<vector<int>> &cumulEnergyCost,
            const vector<bool> &processableIntervals)
                : mTotalProcTime(totalProcTime),
                  mOptCost(Instance::NO_VALUE),
                  mLastLevelOptStart(Instance::NO_VALUE),
                  mOptPath(totalProcTime, vector<int>(numIntervals, Instance::NO_VALUE)),
                  mCostsOnLevels(totalProcTime, vector<int>(numIntervals, Instance::NO_VALUE)),
                  mPermProcTimes(totalProcTime, 1),
                  mPermLevels(totalProcTime, -1),
                  mPermForcedSpaces(totalProcTime, 0),
                  mCostsValidLevel(-1),
                  mCostsValidPosition(-1),
                  mNumIntervals(numIntervals),
                  mEarliestOnIntervalIdx(earliestOnIntervalIdx),
                  mLatestOnIntervalIdx(latestOnIntervalIdx),
                  mOnPowerConsumption(onPowerConsumption),
                  mOptSwitchingCosts(optSwitchingCosts),
                  mOptSwitchingCostsTrans(),
                  mCumulEnergyCost(cumulEnergyCost),
                  mCumulOnEnergyCostPerProcTime(totalProcTime + 1, vector<int>(numIntervals, Instance::NO_VALUE)),
                  mProcessableIntervals(processableIntervals),
                  mIntervalsTmp(numIntervals, Instance::NO_VALUE),
                  mStopwatch() {
        // Transpose of the opt switching costs.
        int optSwitchingCostsRowsCount = optSwitchingCosts.size();
        int optSwitchingCostsColsCount = optSwitchingCosts[0].size();
        mOptSwitchingCostsTrans = vector<vector<int>>(
                optSwitchingCostsColsCount,
                vector<int>(optSwitchingCostsRowsCount, Instance::NO_VALUE));
        for (int row = 0; row < optSwitchingCostsRowsCount; row++) {
            for (int col = 0; col < optSwitchingCostsColsCount; col++) {
                mOptSwitchingCostsTrans[col][row] = mOptSwitchingCosts[row][col];
            }
        }

        for (int procTime = 0; procTime <= totalProcTime; procTime++) {
            for (int startIntervalIdx = 0; startIntervalIdx < mNumIntervals; startIntervalIdx++) {
               int endIntervalIdx = startIntervalIdx + procTime - 1;
               if (endIntervalIdx >= mNumIntervals) {
                   continue;
               }
               mCumulOnEnergyCostPerProcTime[procTime][startIntervalIdx] =
                       mCumulEnergyCost[startIntervalIdx][endIntervalIdx] * mOnPowerConsumption;
            }
        }

        reset();
    }

    void FixedPermCostComputation::reset() {
        mPermProcTimes = vector<int>(mTotalProcTime, 1);
        mPermLevels = vector<int>(mTotalProcTime, -1);
        for (int level = 0; level < mTotalProcTime; level++) {
            mPermLevels[level] = level;
        }
        mPermForcedSpaces = vector<int>(mTotalProcTime, 0);

        mMaxProcessableIntervals = vector<int>(mNumIntervals, 0);

        int nextProcessableIntervalIdx = findNextProcessableInterval(mProcessableIntervals, 0);
        while (nextProcessableIntervalIdx >= 0) {
            int fromIdx = nextProcessableIntervalIdx;
            int toIdx = fromIdx;
            while (toIdx < (int)mProcessableIntervals.size() && mProcessableIntervals[toIdx]) {
                toIdx++;
            }

            toIdx--;

            for (int idx = fromIdx; idx <= toIdx; idx++) {
                mMaxProcessableIntervals[idx] = toIdx - idx + 1;
            }

            nextProcessableIntervalIdx = findNextProcessableInterval(mProcessableIntervals, toIdx + 1);
        }

        invalidateCosts(0);
    }

    int FixedPermCostComputation::findNextProcessableInterval(
            const vector<bool> &processableIntervals,
            int fromIdx) {
        int idx = fromIdx;
        while (idx < (int) processableIntervals.size() && !processableIntervals[idx]) {
            idx++;
        }

        return idx < (int) processableIntervals.size() ? idx : -1;
    }

    void FixedPermCostComputation::join(int fromPosition, int positionsCount) {
        for (int i = 1; i < positionsCount; i++) {
            mPermProcTimes[fromPosition] += mPermProcTimes[fromPosition + i];
        }
        copy(mPermProcTimes.begin() + fromPosition + positionsCount, mPermProcTimes.end(), mPermProcTimes.begin() + fromPosition + 1);
        copy(mPermLevels.begin() + fromPosition + positionsCount, mPermLevels.end(), mPermLevels.begin() + fromPosition + 1);

        mPermProcTimes.resize(mPermProcTimes.size() - positionsCount + 1);
        mPermLevels.resize(mPermLevels.size() - positionsCount + 1);

        this->invalidateCosts(fromPosition);
    }

    void FixedPermCostComputation::split(int fromPosition, int positionsCount) {
        int splitProcTime = mPermProcTimes[fromPosition] / positionsCount;

        int oldPermSize = mPermProcTimes.size();
        mPermProcTimes.resize(oldPermSize + positionsCount - 1);
        mPermLevels.resize(oldPermSize + positionsCount - 1);
        copy_backward(mPermProcTimes.begin() + fromPosition + 1, mPermProcTimes.begin() + oldPermSize, mPermProcTimes.end());
        copy_backward(mPermLevels.begin() + fromPosition + 1, mPermLevels.begin() + oldPermSize, mPermLevels.end());

        int currLevel = fromPosition == 0 ? 0 : mPermLevels[fromPosition];
        for (int i = 0; i < positionsCount; i++) {
            mPermProcTimes[fromPosition + i] = splitProcTime;
            mPermLevels[fromPosition + i] = currLevel;
            currLevel += splitProcTime;
        }

        this->invalidateCosts(fromPosition);
    }

    void FixedPermCostComputation::split(int fromPosition, const vector<int> &procTimes) {
        int oldPermSize = mPermProcTimes.size();

        mPermProcTimes.resize(oldPermSize + procTimes.size() - 1);
        mPermLevels.resize(oldPermSize + procTimes.size() - 1);

        copy_backward(mPermProcTimes.begin() + fromPosition + 1, mPermProcTimes.begin() + oldPermSize, mPermProcTimes.end());
        copy_backward(mPermLevels.begin() + fromPosition + 1, mPermLevels.begin() + oldPermSize, mPermLevels.end());

        int currLevel = fromPosition == 0 ? 0 : mPermLevels[fromPosition];
        for (int i = 0; i < (int)procTimes.size(); i++) {
            mPermProcTimes[fromPosition + i] = procTimes[i];
            mPermLevels[fromPosition + i] = currLevel;
            currLevel += procTimes[i];
        }

        this->invalidateCosts(fromPosition);
    }

    void FixedPermCostComputation::setProcTimes(int fromPosition, int procTime) {
        int numNewPositions = (mTotalProcTime - mPermLevels[fromPosition]) / procTime;

        mPermProcTimes.resize(fromPosition + numNewPositions);
        fill(mPermProcTimes.begin() + fromPosition, mPermProcTimes.end(), procTime);

        mPermLevels.resize(fromPosition + numNewPositions);
        int currLevel = fromPosition == 0 ? 0 : mPermLevels[fromPosition];
        for (int position = fromPosition; position < (int)mPermLevels.size(); position++) {
            mPermLevels[position] = currLevel;
            currLevel += procTime;
        }

        this->invalidateCosts(fromPosition);
    }

    void FixedPermCostComputation::invalidateCosts(int fromPosition) {
        if (fromPosition == 0) {
            mCostsValidLevel = -1;
            mCostsValidPosition = -1;
        }
        else {
            if (mPermLevels[fromPosition - 1] < mCostsValidLevel) {
                mCostsValidLevel = mPermLevels[fromPosition - 1];
                mCostsValidPosition = fromPosition - 1;
            }
        }

        mOptCost = Instance::NO_VALUE;
    }

    int FixedPermCostComputation::recomputeCost() {
        if (mOptCost != Instance::NO_VALUE) {
            return mOptCost;
        }

        mStopwatch.start();

        // prevLevel: the last level having valid costs.
        // currLevel: for this level the costs are computed in the iteration.
        //
        // Recall that "currLevel = total proc time that must be scheduled before the curr level can start".

        int prevLevel = mCostsValidLevel;
        int currLevel = mCostsValidLevel < 0 ? 0 : prevLevel + mPermProcTimes[mCostsValidPosition];

        // From first off.
        if (currLevel == 0) {
            int currProcTime = mPermProcTimes[0];

            auto &currLevelCosts = mCostsOnLevels[currLevel];
            fill(currLevelCosts.begin(), currLevelCosts.end(), Instance::NO_VALUE);

            int currLevelMinStart = mEarliestOnIntervalIdx;
            int currLevelMaxStart = mLatestOnIntervalIdx - mTotalProcTime + 1;

            #pragma omp simd
            for (int currLevelStart = currLevelMinStart; currLevelStart <= currLevelMaxStart; currLevelStart++) {
                int switchingCost = mOptSwitchingCosts[1][currLevelStart];
                int currLevelStartCumulCost = mCumulOnEnergyCostPerProcTime[currProcTime][currLevelStart];
                if (switchingCost >= Instance::NO_VALUE) {
                    currLevelCosts[currLevelStart] = Instance::NO_VALUE;
                }
                else {
                    currLevelCosts[currLevelStart] = switchingCost + currLevelStartCumulCost;
                }
            }

            mCostsValidLevel = 0;
            mCostsValidPosition = 0;
            currLevel = currLevel + currProcTime;
        }

        while (currLevel < mTotalProcTime) {
            int prevProcTime = mPermProcTimes[mCostsValidPosition];
            int currProcTime = mPermProcTimes[mCostsValidPosition + 1];

            prevLevel = currLevel - prevProcTime;

            auto &prevLevelCosts = mCostsOnLevels[prevLevel];
            auto &currLevelCosts = mCostsOnLevels[currLevel];
            fill(currLevelCosts.begin(), currLevelCosts.end(), Instance::NO_VALUE);

            int currLevelMinStart = mEarliestOnIntervalIdx + currLevel;
            int currLevelMaxStart = mLatestOnIntervalIdx - (mTotalProcTime - currLevel) + 1;

            #pragma omp parallel for schedule(dynamic, 1)
            for (int currLevelStart = currLevelMinStart; currLevelStart <= currLevelMaxStart; currLevelStart++) {
                int prevLevelMinStart = mEarliestOnIntervalIdx + prevLevel;
                int prevLevelMaxStart = currLevelStart - prevProcTime - mPermForcedSpaces[mCostsValidPosition];

                int currLevelStartCumulCost = mCumulOnEnergyCostPerProcTime[currProcTime][currLevelStart];

                int minCost = Instance::NO_VALUE;
                int minOptPath = -1;

                for (int prevLevelStart = prevLevelMinStart; prevLevelStart <= prevLevelMaxStart; prevLevelStart++) {
                    int switchingCost = mOptSwitchingCostsTrans[currLevelStart][prevLevelStart + prevProcTime];
                    if (prevLevelCosts[prevLevelStart] != Instance::NO_VALUE
                        && switchingCost != Instance::NO_VALUE
                        && mMaxProcessableIntervals[prevLevelStart] >= prevProcTime) {
                        int cost = prevLevelCosts[prevLevelStart]
                                   + switchingCost
                                   + currLevelStartCumulCost;
                        if (cost < minCost) {
                            minCost = cost;
                            minOptPath = prevLevelStart;
                        }
                    }
                }

                currLevelCosts[currLevelStart] = minCost;
                mOptPath[currLevel][currLevelStart] = minOptPath;
            }

            mCostsValidLevel = currLevel;
            mCostsValidPosition++;
            currLevel = currLevel + currProcTime;
        }

        // To last off.
        mOptCost = Instance::NO_VALUE;
        mLastLevelOptStart = Instance::NO_VALUE;
        {
            prevLevel = mCostsValidLevel;
            int prevProcTime = mPermProcTimes[mCostsValidPosition];
            auto &prevLevelCosts = mCostsOnLevels[prevLevel];

            int prevLevelMinStart = mEarliestOnIntervalIdx + prevLevel;
            int prevLevelMaxStart = mLatestOnIntervalIdx - prevProcTime + 1;

            #pragma omp simd
            for (int prevLevelStart = prevLevelMinStart; prevLevelStart <= prevLevelMaxStart; prevLevelStart++) {
                int switchingCost = mOptSwitchingCostsTrans[mNumIntervals][prevLevelStart + prevProcTime];
                if (prevLevelCosts[prevLevelStart] >= Instance::NO_VALUE
                    || switchingCost >= Instance::NO_VALUE
                    || mMaxProcessableIntervals[prevLevelStart] < prevProcTime) {
                    mIntervalsTmp[prevLevelStart] = Instance::NO_VALUE;
                }
                else {
                    mIntervalsTmp[prevLevelStart] =
                            prevLevelCosts[prevLevelStart]
                            + switchingCost;
                }
            }

            for (int prevLevelStart = prevLevelMinStart; prevLevelStart <= prevLevelMaxStart; prevLevelStart++) {
                int cost = mIntervalsTmp[prevLevelStart];
                if (cost != Instance::NO_VALUE && mOptCost > cost) {
                    mOptCost = cost;
                    mLastLevelOptStart = prevLevelStart;
                }
            }
        }

        mStopwatch.stop();

        return mOptCost;
    }

    vector<int> FixedPermCostComputation::reconstructStartTimes() {
        if (this->recomputeCost() == Instance::NO_VALUE) {
            throw logic_error("Cannot reconstruct start times, does not have feasible schedule.");
        }

        vector<int> permStartTimes(mPermLevels.size(), Instance::NO_VALUE);

        permStartTimes[permStartTimes.size() - 1] = mLastLevelOptStart;
        for (int position = mPermLevels.size() - 1; position > 0; position--) {
            permStartTimes[position - 1] = mOptPath[mPermLevels[position]][permStartTimes[position]];
        }

        return permStartTimes;
    }
}

// This file is released under MIT license.
// See file LICENSE.txt for more information.

#include <optional>
#include <fstream>
#include "SolverConfig.h"

namespace escs {
    SolverConfig::SolverConfig(unsigned long randomSeed, optional<chrono::milliseconds> timeLimit, int numWorkers, vector<int> initialStartTimes)
        : mRandom(randomSeed), mTimeLimit(timeLimit), mNumWorkers(numWorkers), mInitialStartTimes(initialStartTimes)
    {

    }

    SolverConfig SolverConfig::ReadFromPath(string solverConfigPath) {
        ifstream stream;
        stream.open(solverConfigPath);

        long randomSeed;
        stream >> randomSeed;

        long timeLimitInMilliseconds;
        stream >> timeLimitInMilliseconds;
        optional<chrono::milliseconds> timeLimit;
        timeLimit.reset();
        if (timeLimitInMilliseconds > 0) {
            timeLimit = chrono::milliseconds(timeLimitInMilliseconds);
        }

        int numWorkers;
        stream >> numWorkers;

        vector<int> initStartTimes;
        int initStartTimesCount;
        stream >> initStartTimesCount;
        if (initStartTimesCount > 0) {
            initStartTimes = vector<int>(initStartTimesCount, 0);
            for (int i = 0; i < initStartTimesCount; i++) {
                int jobIndex, startTime;
                stream >> jobIndex;
                stream >> startTime;
                initStartTimes[jobIndex] = startTime;
            }
        }

        return SolverConfig(randomSeed, timeLimit, numWorkers, initStartTimes);
    }
}

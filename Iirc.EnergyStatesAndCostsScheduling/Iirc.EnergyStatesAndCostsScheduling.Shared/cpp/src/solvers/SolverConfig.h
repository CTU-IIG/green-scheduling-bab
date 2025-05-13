// This file is released under MIT license.
// See file LICENSE.txt for more information.

#ifndef ENERGYSTATESANDCOSTSSCHEDULING_SOLVERCONFIG_H
#define ENERGYSTATESANDCOSTSSCHEDULING_SOLVERCONFIG_H

#include <optional>
#include <chrono>
#include <string>
#include <random>
#include <map>

namespace escs {
    using namespace std;

    class SolverConfig {
    public:
        mt19937_64 mRandom;
        const optional<chrono::milliseconds> mTimeLimit;
        const int mNumWorkers;
        const vector<int> mInitialStartTimes;

        // TODO: should not be public
        vector<bool> mProcessableIntervals;

        SolverConfig(
                unsigned long randomSeed,
                optional<chrono::milliseconds> timeLimit,
                int numWorkers,
                vector<int> initialStartTimes);

        static SolverConfig ReadFromPath(string solverConfigPath);
    };
}


#endif //ENERGYSTATESANDCOSTSSCHEDULING_SOLVERCONFIG_H

// This file is released under MIT license.
// See file LICENSE.txt for more information.

#include <fstream>
#include "CppInputReader.h"

namespace escs {
    Instance CppInputReader::readFromPath(string instancePath) {
        ifstream stream;
        stream.open(instancePath);

        int machinesCount;
        stream >> machinesCount;

        vector<const Job*> jobs;
        int jobsCount;
        stream >> jobsCount;
        for (int i = 0; i < jobsCount; i++) {
            int id, index, machineIdx, processingTime;
            stream >> id;
            stream >> index;
            stream >> machineIdx;
            stream >> processingTime;
            jobs.push_back(new Job(id, index, machineIdx, processingTime));
        }

        vector<const Interval*> intervals;
        int intervalsCount;
        stream >> intervalsCount;
        for (int i = 0; i < intervalsCount; i++) {
            int index, start, end, energyCost;
            stream >> index;
            stream >> start;
            stream >> end;
            stream >> energyCost;
            intervals.push_back(new Interval(index, start, end, energyCost));
        }

        int lengthInterval;
        stream >> lengthInterval;

        int onPowerConsumption;
        stream >> onPowerConsumption;

        int earliestOnIntervalIdx;
        stream >> earliestOnIntervalIdx;

        int latestOnIntervalIdx;
        stream >> latestOnIntervalIdx;

        vector<vector<int>> optimalSwitchingCosts;
        {
            int rowsCount, colsCount;
            stream >> rowsCount;
            stream >> colsCount;

            optimalSwitchingCosts = vector<vector<int>>(rowsCount, vector<int>(colsCount, Instance::NO_VALUE));

            for (int row = 0; row < rowsCount; row++) {
                for (int col = 0; col < colsCount; col++) {
                    int value;
                    stream >> value;
                    if (value >= 0) {
                        optimalSwitchingCosts[row][col] = value;
                    }
                }
            }
        }

        vector<vector<int>> fullOptimalSwitchingCosts;
        {
            int rowsCount, colsCount;
            stream >> rowsCount;
            stream >> colsCount;

            fullOptimalSwitchingCosts = vector<vector<int>>(rowsCount, vector<int>(colsCount, Instance::NO_VALUE));

            for (int row = 0; row < rowsCount; row++) {
                for (int col = 0; col < colsCount; col++) {
                    int value;
                    stream >> value;
                    if (value >= 0) {
                        fullOptimalSwitchingCosts[row][col] = value;
                    }
                }
            }
        }

        vector<vector<int>> cumulativeEnergyCost;
        {
            int rowsCount, colsCount;
            stream >> rowsCount;
            stream >> colsCount;

            cumulativeEnergyCost = vector<vector<int>>(rowsCount, vector<int>(colsCount, Instance::NO_VALUE));

            for (int row = 0; row < rowsCount; row++) {
                for (int col = 0; col < colsCount; col++) {
                    int value;
                    stream >> value;
                    cumulativeEnergyCost[row][col] = value;
                }
            }
        }

        return Instance(
                machinesCount,
                jobs,
                intervals,
                lengthInterval,
                onPowerConsumption,
                earliestOnIntervalIdx,
                latestOnIntervalIdx,
                optimalSwitchingCosts,
                fullOptimalSwitchingCosts,
                cumulativeEnergyCost);
    }
}

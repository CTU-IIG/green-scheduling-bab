// This file is released under MIT license.
// See file LICENSE.txt for more information.

#ifndef ENERGYSTATESANDCOSTSSCHEDULING_JOB_H
#define ENERGYSTATESANDCOSTSSCHEDULING_JOB_H


namespace escs {
    class Job {
    public:
        const int mId;
        const int mIndex;
        const int mMachineIdx;
        const int mProcessingTime;

        Job(int id, int index, int machineIdx, int processingTime);
    };
}

#endif //ENERGYSTATESANDCOSTSSCHEDULING_JOB_H

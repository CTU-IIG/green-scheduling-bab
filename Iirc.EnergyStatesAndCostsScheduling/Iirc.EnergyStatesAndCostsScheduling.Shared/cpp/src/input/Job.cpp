// This file is released under MIT license.
// See file LICENSE.txt for more information.

#include "Job.h"

namespace escs {
    Job::Job(int id, int index, int machineIdx, int processingTime)
        : mId(id), mIndex(index), mMachineIdx(machineIdx), mProcessingTime(processingTime) {}
}

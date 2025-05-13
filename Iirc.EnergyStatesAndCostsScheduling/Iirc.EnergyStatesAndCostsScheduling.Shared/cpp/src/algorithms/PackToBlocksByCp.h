// This file is released under MIT license.
// See file LICENSE.txt for more information.

#ifndef ENERGYSTATESANDCOSTSSCHEDULING_PACKTOBLOCKSBYCP_H
#define ENERGYSTATESANDCOSTSSCHEDULING_PACKTOBLOCKSBYCP_H

#include <map>

#include "../datastructs/Block.h"
#include "../datastructs/FixedPermCostComputation.h"

namespace escs {
    // TODO: would be great to use in BaB? Or do we really need it?
    class PackToBlocksByCp {
    public:
        vector<int> mPermProcTimes;
        vector<int> mPermStartTimes;

        PackToBlocksByCp();

        bool solve(
                const vector<Block> &blocks,
                const vector<int> &procTimes,
                optional<chrono::milliseconds> timeLimit);
    };
}

#endif //ENERGYSTATESANDCOSTSSCHEDULING_PACKTOBLOCKSBYCP_H

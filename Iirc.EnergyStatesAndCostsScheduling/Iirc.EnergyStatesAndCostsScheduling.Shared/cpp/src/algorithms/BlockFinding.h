// This file is released under MIT license.
// See file LICENSE.txt for more information.

#ifndef ENERGYSTATESANDCOSTSSCHEDULING_BLOCKFINDING_H
#define ENERGYSTATESANDCOSTSSCHEDULING_BLOCKFINDING_H

#include <map>
#include <gurobi_c++.h>

#include "../datastructs/Block.h"
#include "../datastructs/FixedPermCostComputation.h"

namespace escs {
    class BlockFinding {
    private:
        const GRBEnv &mEnv;

        void solveMinimizeLengthDifference(
                const Instance &instance,
                const vector<Block> &blocks,
                optional<chrono::milliseconds> timeLimit);

    public:
        enum BlockFindingStrategy
        {
            MinimizeLengthDifference = 0
        };

        vector<int> mAssignments;
        bool mSolutionSameAsBlocks;

        BlockFinding(const GRBEnv &env);

        void solve(
                BlockFindingStrategy strategy,
                const Instance &instance,
                const vector<Block> &blocks,
                optional<chrono::milliseconds> timeLimit);
    };
}

#endif //ENERGYSTATESANDCOSTSSCHEDULING_BLOCKFINDING_H

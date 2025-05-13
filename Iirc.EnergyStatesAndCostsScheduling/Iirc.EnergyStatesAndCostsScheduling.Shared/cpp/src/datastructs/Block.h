// This file is released under MIT license.
// See file LICENSE.txt for more information.

#ifndef ENERGYSTATESANDCOSTSSCHEDULING_BLOCK_H
#define ENERGYSTATESANDCOSTSSCHEDULING_BLOCK_H

#include <vector>
#include "../datastructs/FixedPermCostComputation.h"

using namespace std;

namespace escs {
    class Block {
    public:
        int mStart;
        int mCompletion;

        Block(int start, int completion): mStart(start), mCompletion(completion) {}

        int getLength() const {
            return mCompletion - mStart;
        }

        static vector<Block> getProcBlocks(FixedPermCostComputation &fixedPermCostComputation, int fromPosition) {
            auto startTimes = fixedPermCostComputation.reconstructStartTimes();
            auto &permProcTimes = fixedPermCostComputation.getPermProcTimes();
            return Block::getProcBlocks(startTimes, permProcTimes, fromPosition);
        }

        static vector<Block> getProcBlocks(
                const vector<int> &startTimes,
                const vector<int> &permProcTimes,
                int fromPosition) {
            vector<Block> blocks;
            for (int position = fromPosition; position < (int)permProcTimes.size(); position++) {
                int blockStart = startTimes[position];
                int blockCompletion = blockStart + permProcTimes[position];

                if (blocks.empty()) {
                    blocks.emplace_back(blockStart, blockCompletion);
                }
                else {
                    if (blocks.back().mCompletion < blockStart) {
                        // Idle between blocks.
                        blocks.emplace_back(blockStart, blockCompletion);
                    }
                    else {
                        // Merge blocks.
                        blocks.back().mCompletion = blockCompletion;
                    }
                }
            }

            return blocks;
        }
    };
}

#endif //ENERGYSTATESANDCOSTSSCHEDULING_BLOCK_H

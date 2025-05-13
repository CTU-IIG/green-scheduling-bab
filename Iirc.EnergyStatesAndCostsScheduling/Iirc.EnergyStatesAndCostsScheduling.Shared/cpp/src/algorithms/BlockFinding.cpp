// This file is released under MIT license.
// See file LICENSE.txt for more information.

#include <ilcp/cp.h>
#include <algorithm>
#include "BlockFinding.h"

namespace escs {
    BlockFinding::BlockFinding(const GRBEnv &env): mEnv(env) {

    }

    void BlockFinding::solve(
            BlockFindingStrategy strategy,
            const Instance &instance,
            const vector<Block> &blocks,
            optional<chrono::milliseconds> timeLimit) {
        // Clear.
        mAssignments.clear();
        mSolutionSameAsBlocks = false;

        switch (strategy) {
            case MinimizeLengthDifference:
                solveMinimizeLengthDifference(instance, blocks, timeLimit);
                break;
        }
    }

    void BlockFinding::solveMinimizeLengthDifference(
            const Instance &instance,
            const vector<Block> &blocks,
            optional<chrono::milliseconds> timeLimit)
    {
        GRBModel model(mEnv);
        model.set("OutputFlag","0");

        if (timeLimit.has_value())
        {
            double timeLimitInSeconds = ((double)timeLimit.value().count()) / 1000.0;
            model.set(GRB_DoubleParam_TimeLimit, timeLimitInSeconds);
        }

        // - add variables
        GRBVar z = model.addVar(0, instance.getTotalProcTime(), 1.0, GRB_CONTINUOUS, "z");
        vector<GRBVar> s;
        auto x_jb = vector<vector<GRBVar>>(instance.mJobs.size(), vector<GRBVar>());

        for (int j = 0; j < (int)instance.mJobs.size(); j++) {
            for (int b = 0; b < (int)blocks.size(); b++) {
                x_jb[j].push_back(model.addVar(0, 1, 0.0, GRB_BINARY, "x" + to_string(j) + "_" +  to_string(b)));
            }
        }

        for (int b = 0; b < (int)blocks.size(); b++) {
            s.push_back(model.addVar(0, instance.getTotalProcTime(), 0.0, GRB_INTEGER, "s" + to_string(b)));
        }

        model.update();

        // add constraints
        for (int j = 0; j < (int)instance.mJobs.size(); j++) {
            GRBLinExpr b_sum = 0;
            for (int b = 0; b < (int)blocks.size(); b++) {
                b_sum += x_jb[j][b];
            }

            model.addConstr(b_sum == 1, "job_ass_" + to_string(j));
        }

        for (int b = 0; b < (int)blocks.size(); b++) {
            GRBLinExpr b_sum = 0;
            for (int j = 0; j < (int)instance.mJobs.size(); j++) {
                b_sum += x_jb[j][b] * instance.mJobs[j]->mProcessingTime;
            }

            model.addConstr(s[b] == b_sum, "block_size_" + to_string(b));

            model.addConstr(z >= s[b] - blocks[b].getLength(), "z1_" + to_string(b));
            model.addConstr(z >= blocks[b].getLength() - s[b], "z2_" + to_string(b));
        }

        model.update();
        model.optimize();

        // Handling disappeared blocks -> continuous indices.
        int usedBlocksCount = 0;
        auto blocksMapping = vector<int>(blocks.size(), -1);
        for (int b = 0; b < (int)blocks.size(); b++) {
            if (s[b].get(GRB_DoubleAttr_X) >= 0.5) {
                blocksMapping[b] = usedBlocksCount;
                usedBlocksCount++;
            }
        }

        if (model.get(GRB_IntAttr_SolCount) > 0) {
            for (int j = 0; j < (int)instance.mJobs.size(); j++) {
                for (int b = 0; b < (int)blocks.size(); b++) {
                    if (x_jb[j][b].get(GRB_DoubleAttr_X) >= 0.5) {
                        mAssignments.push_back(blocksMapping[b]);
                        break;
                    }
                }
            }
        }

        assert(mAssignments.size() == instance.mJobs.size());

        mSolutionSameAsBlocks = z.get(GRB_DoubleAttr_X) <= 0.1;
    }
}

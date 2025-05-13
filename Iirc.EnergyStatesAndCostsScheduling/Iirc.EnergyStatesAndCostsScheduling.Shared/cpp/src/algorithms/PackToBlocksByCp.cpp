// This file is released under MIT license.
// See file LICENSE.txt for more information.

#include <ilcp/cp.h>
#include <algorithm>
#include "PackToBlocksByCp.h"

namespace escs {
    PackToBlocksByCp::PackToBlocksByCp() {

    }

    bool PackToBlocksByCp::solve(
            const vector<Block> &blocks,
            const vector<int> &procTimes,
            optional<chrono::milliseconds> timeLimit) {
        mPermStartTimes = vector<int>();
        mPermProcTimes = vector<int>();

        // Construct CP model.
        IloEnv env;
        IloModel model(env);

        IloIntVarArray load(env);
        for (auto &block : blocks) {
            load.add(IloIntVar(env, block.getLength()));
        }

        IloIntArray size(env);
        for (auto procTime : procTimes) {
            size.add(procTime);
        }

        IloIntVarArray where(env, size.getSize(), 0, load.getSize() - 1);
        model.add(IloPack(env, load, where, size));

        IloCP cp(model);

        if (timeLimit.has_value() && timeLimit.value().count() > 0) {
            float timeLimitInSeconds = (1.0 * timeLimit.value().count()) / 1000.0;
            cp.setParameter(IloCP::TimeLimit, timeLimitInSeconds);
        }
        cp.setParameter(IloCP::LogVerbosity, IloCP::Quiet);
        if (cp.solve()) {
            // Solution found, reconstruct start times.

            vector<int> blockNextStarts;
            for (auto &block : blocks) {
                blockNextStarts.push_back(block.mStart);
            }

            vector<pair<int, int>> procTimeWithStart; // (procTime, startTime)
            for (int i = 0; i < size.getSize(); i++) {
                int procTime = size[i];
                int blockIdx = cp.getValue(where[i]);
                int startTime = blockNextStarts[blockIdx];
                procTimeWithStart.push_back(make_pair(procTime, startTime));
                blockNextStarts[blockIdx] = startTime + procTime;
            }

            sort(
                    procTimeWithStart.begin(),
                    procTimeWithStart.end(),
                    [&](const pair<int, int> &lhs, const pair<int, int> &rhs) {
                        return lhs.second < rhs.second;
                    });

            for (auto &p : procTimeWithStart) {
                mPermProcTimes.push_back(p.first);
                mPermStartTimes.push_back(p.second);
            }

            env.end();
            return true;
        }

        env.end();
        return false;
    }
}

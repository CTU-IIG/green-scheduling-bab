// This file is released under MIT license.
// See file LICENSE.txt for more information.

#ifndef ENERGYSTATESANDCOSTSSCHEDULING_STATUS_H
#define ENERGYSTATESANDCOSTSSCHEDULING_STATUS_H

namespace escs {
    enum Status {
        NoSolution = 0,
        Optimal = 1,
        Infeasible = 2,
        Heuristic = 3
    };
}

#endif //ENERGYSTATESANDCOSTSSCHEDULING_STATUS_H

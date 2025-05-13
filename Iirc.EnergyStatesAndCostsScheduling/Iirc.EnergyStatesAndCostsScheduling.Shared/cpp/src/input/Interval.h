// This file is released under MIT license.
// See file LICENSE.txt for more information.

#ifndef ENERGYSTATESANDCOSTSSCHEDULING_INTERVAL_H
#define ENERGYSTATESANDCOSTSSCHEDULING_INTERVAL_H

namespace escs {
    class Interval {
    public:
        const int mIndex;
        const int mStart;
        const int mEnd;
        const int mEnergyCost;

        Interval(int index, int start, int end, int energyCost);
    };
}


#endif //ENERGYSTATESANDCOSTSSCHEDULING_INTERVAL_H

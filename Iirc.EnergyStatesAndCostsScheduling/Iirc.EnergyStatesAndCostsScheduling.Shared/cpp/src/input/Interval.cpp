// This file is released under MIT license.
// See file LICENSE.txt for more information.

#include "Interval.h"

namespace escs {
    Interval::Interval(int index, int start, int end, int energyCost)
        : mIndex(index), mStart(start), mEnd(end), mEnergyCost(energyCost) {}
}

// This file is released under MIT license.
// See file LICENSE.txt for more information.

#ifndef ENERGYSTATESANDCOSTSSCHEDULING_GCDOFVALUES_H
#define ENERGYSTATESANDCOSTSSCHEDULING_GCDOFVALUES_H

#include <vector>

using namespace std;

namespace escs {
    class GcdOfValues {
    private:
        vector<vector<int>> mGcd;

        int computeGcdTrace(int a, int b);

    public:
        GcdOfValues(vector<int> allValues);

        int gcd(const vector<int> &values) ;
    };
}



#endif //ENERGYSTATESANDCOSTSSCHEDULING_GCDOFVALUES_H

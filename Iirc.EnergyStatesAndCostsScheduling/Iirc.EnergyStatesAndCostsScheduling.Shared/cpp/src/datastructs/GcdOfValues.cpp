// This file is released under MIT license.
// See file LICENSE.txt for more information.

#include <algorithm>
#include <vector>
#include <set>
#include "GcdOfValues.h"

using namespace std;

namespace escs {

    GcdOfValues::GcdOfValues(vector<int> allValues) : mGcd()
    {
        // Replace duplicates in allValues
        {
            set<int> setAllValues(allValues.begin(), allValues.end());
            allValues = vector<int>(setAllValues.begin(), setAllValues.end());
        }

        int maxValue = *(max_element(allValues.begin(), allValues.end()));
        mGcd = vector<vector<int>>(maxValue + 1, vector<int>(maxValue + 1, -1));

        // Compute the gcd of every ordered pair.
        // Gcd is computed using Euclidean algorithm (modulo version) with caching of intermediate gcds. This way,
        // the computation of gcd for all pairs should be faster since we can lookup the already computed gcds.
        for (int i = 0; i < (int)allValues.size(); i++) {
            int iValue = allValues[i];
            mGcd[iValue][iValue] = iValue;

            for (int j = i + 1; j < (int)allValues.size(); j++) {
                int jValue = allValues[j];
                if (iValue >= jValue) {
                    computeGcdTrace(iValue, jValue);
                }
                else {
                    computeGcdTrace(jValue, iValue);
                }
            }
        }
    }

    int GcdOfValues::computeGcdTrace(int a, int b) {
        // Expects: a >= b
        int gcd = mGcd[a][b];

        if (gcd >= 1) {
            // Already computed.
            return gcd;
        }

        if (b == 0) {
            gcd = a;
            mGcd[a][b] = gcd;
            mGcd[b][a] = gcd;
            return a;
        }

        gcd = computeGcdTrace(b, a % b);
        mGcd[a][b] = gcd;
        mGcd[b][a] = gcd;
        return gcd;
    }

    int GcdOfValues::gcd(const vector<int> &values) {
        // The values must be a subset of allValues passed in constructor.

        // Exploits the following property
        // gcd(a, b, c) = gcd(gcd(a, b), c)
        int currGcd = values[0];
        for (int i = 1; i < (int)values.size(); i++) {
            if (currGcd == 1) {
                // No need to test more values.
                return currGcd;
            }

            int value = values[i];

            if (currGcd >= value) {
                currGcd = computeGcdTrace(currGcd, value);
            }
            else {
                currGcd = computeGcdTrace(value, currGcd);
            }
        }

        return currGcd;
    }
}

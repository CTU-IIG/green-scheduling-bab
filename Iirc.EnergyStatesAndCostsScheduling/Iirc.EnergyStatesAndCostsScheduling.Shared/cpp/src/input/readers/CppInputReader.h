// This file is released under MIT license.
// See file LICENSE.txt for more information.

#ifndef ENERGYSTATESANDCOSTSSCHEDULING_CPPINPUTREADER_H
#define ENERGYSTATESANDCOSTSSCHEDULING_CPPINPUTREADER_H

#include "../Instance.h"

using namespace std;

namespace escs {

    class CppInputReader {
    public:
        CppInputReader() {}

        Instance readFromPath(string instancePath);
    };
}


#endif //ENERGYSTATESANDCOSTSSCHEDULING_CPPINPUTREADER_H

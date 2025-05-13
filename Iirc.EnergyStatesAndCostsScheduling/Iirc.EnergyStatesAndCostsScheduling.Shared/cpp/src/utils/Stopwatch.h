// This file is released under MIT license.
// See file LICENSE.txt for more information.

#ifndef ENERGYSTATESANDCOSTSSCHEDULING_STOPWATCH_H
#define ENERGYSTATESANDCOSTSSCHEDULING_STOPWATCH_H

#include <optional>
#include <chrono>

using namespace std;

namespace escs {

    class Stopwatch {
    private:
        chrono::time_point<chrono::steady_clock> mStart;
        chrono::time_point<chrono::steady_clock> mEnd;
        bool mRunning;
        chrono::milliseconds mAccDuration;

    public:
        Stopwatch();

        void start();

        void stop();

        chrono::milliseconds totalDuration() const;
        chrono::milliseconds duration() const;

        bool timeLimitReached(const optional<chrono::milliseconds> &timeLimit) const;

        optional<chrono::milliseconds> remainingTime(const optional<chrono::milliseconds> &timeLimit) const;
    };

}


#endif //ENERGYSTATESANDCOSTSSCHEDULING_STOPWATCH_H

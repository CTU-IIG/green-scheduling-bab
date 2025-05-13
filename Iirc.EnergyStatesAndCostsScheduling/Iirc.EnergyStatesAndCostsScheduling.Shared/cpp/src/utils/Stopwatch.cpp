// This file is released under MIT license.
// See file LICENSE.txt for more information.

#include "Stopwatch.h"

namespace escs {

    Stopwatch::Stopwatch() : mStart(chrono::milliseconds::zero()), mEnd(chrono::milliseconds::zero()), mRunning(false), mAccDuration(0) {}

    void Stopwatch::start() {
        if (!mRunning) {
            mStart = chrono::steady_clock::now();
            mRunning = true;
        }
    }

    void Stopwatch::stop() {
        if (mRunning) {
            mEnd = chrono::steady_clock::now();
            mRunning = false;
            mAccDuration += duration();
        }
    }

    chrono::milliseconds Stopwatch::duration() const {
        auto end = mRunning ? chrono::steady_clock::now() : mEnd;
        return chrono::duration_cast<chrono::milliseconds>(end - mStart);
    }

    chrono::milliseconds Stopwatch::totalDuration() const {
        return mRunning ? mAccDuration + duration() : mAccDuration;
    }

    bool Stopwatch::timeLimitReached(const optional<chrono::milliseconds> &timeLimit) const {
        return timeLimit.has_value() ? (totalDuration() > timeLimit.value()) : false;
    }

    optional<chrono::milliseconds> Stopwatch::remainingTime(const optional<chrono::milliseconds> &timeLimit) const {
        if (timeLimit.has_value()) {
            auto totalDurationValue = totalDuration();
            if (timeLimit.value().count() <= totalDurationValue.count()) {
                return optional<chrono::milliseconds>(0);
            }
            else {
                return optional<chrono::milliseconds>(timeLimit.value() - totalDurationValue);
            }
        }
        else {
            return optional<chrono::milliseconds>();
        }
    }
}

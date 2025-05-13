// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="Timer.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.Utils.Time
{
    using System;
    using System.Diagnostics;

    public class Timer
    {
        public TimeSpan? TimeLimit { get; private set; }

        private Stopwatch stopwatch;

        public Timer(TimeSpan? timeLimit)
        {
            this.TimeLimit = timeLimit;
            this.stopwatch = new Stopwatch();
        }

        public void Start()
        {
            this.stopwatch.Start();
        }

        public void Stop()
        {
            this.stopwatch.Stop();
        }

        public void Restart()
        {
            this.stopwatch.Restart();
        }

        public void Reset()
        {
            this.stopwatch.Reset();
        }

        public TimeSpan? RemainingTime
        {
            get
            {
                if (this.TimeLimit.HasValue == false)
                {
                    return null;
                }

                var delta = this.TimeLimit.Value - this.stopwatch.Elapsed;
                return delta < TimeSpan.Zero ? TimeSpan.Zero : delta;
            }
        }

        public bool TimeLimitReached
        {
            get
            {
                if (this.TimeLimit.HasValue == false)
                {
                    return false;
                }

                return this.RemainingTime == TimeSpan.Zero;
            }
        }

        public long ElapsedMilliseconds
        {
            get
            {
                return this.stopwatch.ElapsedMilliseconds;
            }
        }
    }
}

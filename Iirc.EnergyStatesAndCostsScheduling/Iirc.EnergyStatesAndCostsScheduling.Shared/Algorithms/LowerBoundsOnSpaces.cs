// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;
    using Iirc.Utils.SolverFoundations;
    using Iirc.Utils.Time;
    
    public class LowerBoundsOnSpaces : IAlgorithm
    {
        private readonly ExtendedInstance instance;
        private readonly List<Job> jobsOrderedByProcessingTime;
        private readonly int totalProcessingTime;
        private readonly int shortestProcessingTime;
        private readonly List<Interval> intervalsOrderedByCost;

        private Timer timer;

        public LowerBoundsOnSpaces(ExtendedInstance instance)
        {
            this.instance = instance;
            jobsOrderedByProcessingTime = instance.Jobs.OrderBy(job => job.ProcessingTime).ToList();
            shortestProcessingTime = jobsOrderedByProcessingTime.First().ProcessingTime;
            totalProcessingTime = instance.Jobs.Sum(job => job.ProcessingTime);
            intervalsOrderedByCost = instance.Intervals
                                    .Where(interval => interval.Index >= instance.EarliestOnIntervalIdx &&
                                           interval.Index <= instance.LatestOnIntervalIdx)
                                    .OrderBy(interval => interval.EnergyCost).ToList();

        }

        public int?[][] GapsLowerBounds { get; private set; }

        public Status Solve(TimeSpan? timeLimit = null)
        {
            timer = new Timer(timeLimit);
            timer.Start();

            Clear();

            // TODO: disable parallel computation for single-threaded programs?
            Parallel.ForEach(
                IntervalsPairsEnumerator(),
                new ParallelOptions {MaxDegreeOfParallelism = -1},
                intervalsPair =>
                {
                    var beginIntervalIdx = intervalsPair.Item1;
                    var endIntervalIdx = intervalsPair.Item2;

                    SolveForIntervals(beginIntervalIdx, endIntervalIdx);
                });

            timer.Stop();
            return TimeLimitReached ? Status.NoSolution : Status.Optimal;
        }

        public bool TimeLimitReached => timer.TimeLimitReached;

        private IEnumerable<Tuple<int, int>> IntervalsPairsEnumerator()
        {
            for (var beginIntervalIdx = 0; beginIntervalIdx <= instance.Intervals.Length; beginIntervalIdx++)
            for (var endIntervalIdx = beginIntervalIdx;
                endIntervalIdx <= instance.Intervals.Length;
                endIntervalIdx++)
                yield return new Tuple<int, int>(beginIntervalIdx, endIntervalIdx);
        }

        private void Clear()
        {
            GapsLowerBounds = Enumerable
                .Repeat((int?) null, instance.Intervals.Length + 1)
                .Select(_ => new int?[instance.Intervals.Length + 1])
                .ToArray();
        }

        /// <summary>
        /// </summary>
        /// <param name="beginIntervalIdx">Begin interval (inclusive).</param>
        /// <param name="endIntervalIdx">End interval (exclusive).</param>
        private void SolveForIntervals(int beginIntervalIdx, int endIntervalIdx)
        {
            // is OptimalSwitchingCosts gives null, gap is not feasible, LB = infinity
            if (instance.OptimalSwitchingCosts[beginIntervalIdx][endIntervalIdx] == null)
            {
                GapsLowerBounds[beginIntervalIdx][endIntervalIdx] = int.MaxValue;
                return;
            }

            // TODO: Do it even for 1 job
            if (instance.Jobs.Length < 2)
            {
                GapsLowerBounds[beginIntervalIdx][endIntervalIdx] = 
                    instance.OptimalSwitchingCosts[beginIntervalIdx][endIntervalIdx];
                return;
            }
            
            bool leftOccupied = beginIntervalIdx - shortestProcessingTime < instance.EarliestOnIntervalIdx;
            bool rightOccupied = endIntervalIdx + shortestProcessingTime - 1 > instance.LatestOnIntervalIdx;
            int lastIdx = instance.Intervals.Length - 1;
            if ((beginIntervalIdx == 1 && rightOccupied) ||
                (endIntervalIdx == lastIdx && leftOccupied) ||
                (beginIntervalIdx > 1 && endIntervalIdx < lastIdx && (leftOccupied || rightOccupied)))
            {
                GapsLowerBounds[beginIntervalIdx][endIntervalIdx] = int.MaxValue;
                return;
            }

            var lb = instance.OptimalSwitchingCosts[beginIntervalIdx][endIntervalIdx];
            var remainingProcessingTime = totalProcessingTime;
            var filteredIntervals = intervalsOrderedByCost.Where(
                interval => (interval.Index < beginIntervalIdx - shortestProcessingTime ||
                             interval.Index >= endIntervalIdx + shortestProcessingTime)).ToList();  
                
            // put the shortest task to the left
            if (beginIntervalIdx > 1)
            {
                lb += this.instance.CumulativeEnergyCost[beginIntervalIdx - this.shortestProcessingTime][beginIntervalIdx - 1] * this.instance.OnPowerConsumption;
                remainingProcessingTime -= shortestProcessingTime;
            }
            
            // put the shortest task to the right
            if (endIntervalIdx < lastIdx)
            {
                lb += this.instance.CumulativeEnergyCost[endIntervalIdx][endIntervalIdx + this.shortestProcessingTime - 1] * this.instance.OnPowerConsumption;
                remainingProcessingTime -= shortestProcessingTime;
            }

            // check if remaining jobs can be preemptively scheduled
            if (filteredIntervals.Count < remainingProcessingTime)
            {
                GapsLowerBounds[beginIntervalIdx][endIntervalIdx] = int.MaxValue;
                return;
            }

            // process the remaining jobs preemptively during the remaining cheapest periods
            for (var i = 0; i < remainingProcessingTime; i++)
            {
                lb += filteredIntervals[i].EnergyCost * instance.OnPowerConsumption;
            }

            GapsLowerBounds[beginIntervalIdx][endIntervalIdx] = lb;
        }
    }
}
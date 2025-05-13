// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;
    using Iirc.Utils.Graph;
    using Iirc.Utils.SolverFoundations;
    using Iirc.Utils.Time;

    /// <summary>
    /// SPACES algorithm introduced in [Benedikt2020a].
    /// </summary>
    public class ShortestPathAlgorithmCostEfficientSwitchings : IAlgorithm
    {
        private ExtendedInstance instance;

        private Timer timer;
        private int totalProcessingTime;
        private List<Job> jobsOrderedByProcessingTime;

        public ShortestPathAlgorithmCostEfficientSwitchings(ExtendedInstance instance)
        {
            this.instance = instance;
            this.totalProcessingTime = instance.Jobs.Sum(job => job.ProcessingTime);
            this.jobsOrderedByProcessingTime = instance.Jobs.OrderBy(job => job.ProcessingTime).ToList();
        }
        
        public Status Solve(TimeSpan? timeLimit = null)
        {
            this.timer = new Timer(timeLimit);
            this.timer.Start();
            
            this.Clear();

            this.ComputeOptimalSwitchingCosts();
            
            this.timer.Stop();
            return this.TimeLimitReached ? Status.NoSolution : Status.Optimal;
        }

        private void Clear()
        {
            this.OptimalCosts = Enumerable
                .Repeat(0, this.instance.Intervals.Length + 1)
                .Select(_ => new int?[this.instance.Intervals.Length + 1])
                .ToArray();
            
            this.FullOptimalCosts = Enumerable
                .Repeat(0, this.instance.Intervals.Length + 1)
                .Select(_ => new int?[this.instance.Intervals.Length + 1])
                .ToArray();
            
            this.OptimalStates = Enumerable
                .Repeat(0, this.instance.Intervals.Length + 1)
                .Select(_ => Enumerable.Repeat<List<int>>(null, this.instance.Intervals.Length + 1).ToArray())
                .ToArray();
        }

        /// <summary>
        /// Simple feasibility check: is it possible to schedule all the jobs outside of
        /// [beginIntervalIdx, endIntervalIdx)?
        /// </summary>
        /// <param name="beginIntervalIdx"></param>
        /// <param name="endIntervalIdx"></param>
        /// <param name="sourceStateIdx"></param>
        /// <param name="sinkStateIdx"></param>
        /// <returns></returns>
        private bool FeasibilityCheck(int beginIntervalIdx, int endIntervalIdx, int sourceStateIdx, int sinkStateIdx)
        {
            int remainingTimeForProcessingLeft = Math.Max(0, beginIntervalIdx - this.instance.EarliestOnIntervalIdx);
            if (sourceStateIdx == this.instance.OnStateIdx && remainingTimeForProcessingLeft == 0)
            {
                return false;    // At least one on interval to the left.
            }
            
            int remainingTimeForProcessingRight = Math.Max(0, (this.instance.LatestOnIntervalIdx + 1) - endIntervalIdx);
            if (sinkStateIdx == this.instance.OnStateIdx && remainingTimeForProcessingRight == 0)
            {
                return false;    // At least one on interval to the right.
            }
            
            if (this.jobsOrderedByProcessingTime.Any())
            {
                // The gap interval is not feasible if the largest job cannot be scheduled to the left or to the right
                // of the gap.
                var largestJob = this.jobsOrderedByProcessingTime.Last();
                if (largestJob.ProcessingTime > remainingTimeForProcessingLeft &&
                    largestJob.ProcessingTime > remainingTimeForProcessingRight)
                {
                    return false;
                }
            }
                
            return (remainingTimeForProcessingLeft + remainingTimeForProcessingRight) >= this.totalProcessingTime;
        }

        private void ComputeOptimalSwitchingCosts()
        {
            var graph = this.ConstructFullGraph();
            
            // TODO: disable parallel computation for single-threaded programs?
            Parallel.ForEach(
                this.instance.Intervals,
                new ParallelOptions {MaxDegreeOfParallelism = -1},
                beginInterval =>
                {
                    int beginStateIdx = beginInterval.Index <= 1 ? this.instance.BaseOffStateIdx : this.instance.OnStateIdx;
                    
                    var shortestPaths = new ShortestPaths(graph.NodesCount);
                    shortestPaths.SetInput(
                        graph, 
                        false,
                        false,
                        graph.NodeIndex(beginInterval.Index, beginStateIdx));
                    if (shortestPaths.Solve(this.timer.RemainingTime).IsFeasibleSolution())
                    {
                        for (int endIntervalIdx = beginInterval.Index;
                            endIntervalIdx <= this.instance.Intervals.Length;
                            endIntervalIdx++)
                        {
                            int endStateIdx = endIntervalIdx >= (this.instance.Intervals.Length - 1) ?
                                this.instance.BaseOffStateIdx
                                : this.instance.OnStateIdx;
                            
                            int pathWeight = int.MaxValue;
                            if (endIntervalIdx == this.instance.Intervals.Length)
                            {
                                pathWeight = shortestPaths.PathWeights[graph.Sink];
                            }
                            else
                            {
                                pathWeight = shortestPaths.PathWeights[graph.NodeIndex(endIntervalIdx, endStateIdx)];
                            }
                                
                            this.FullOptimalCosts[beginInterval.Index][endIntervalIdx] =
                                pathWeight == int.MaxValue ? (int?)null : pathWeight;
                            
                            if (this.FeasibilityCheck(beginInterval.Index, endIntervalIdx, beginStateIdx, endStateIdx))
                            {
                                this.OptimalCosts[beginInterval.Index][endIntervalIdx] =
                                    this.FullOptimalCosts[beginInterval.Index][endIntervalIdx];
                            }
                        }
                    }
                });
        }
        
        private LayeredGraph ConstructFullGraph()
        {
            var graph = new LayeredGraph( 
                this.instance.States.Length, 
                 this.instance.Intervals.Length);

            // Inner edges.
            for (int fromIntervalIdx = 0; fromIntervalIdx < this.instance.Intervals.Length; fromIntervalIdx++)
            {
                if (fromIntervalIdx == 0)
                {
                    // Only edge between base off from interval 0 to interval 1.
                    this.AddEdge(
                        graph,
                        fromIntervalIdx,
                        this.instance.BaseOffStateIdx,
                        this.instance.BaseOffStateIdx);
                    continue;
                }
                
                for (int fromStateIdx = 0; fromStateIdx < this.instance.States.Length; fromStateIdx++)
                {
                    for (int toStateIdx = 0; toStateIdx < this.instance.States.Length; toStateIdx++)
                    {
                        this.AddEdge(graph, fromIntervalIdx, fromStateIdx, toStateIdx);
                    }
                }
            }
            
            // Edge to sink from the last base off state.
            graph.AddEdgeToSink(
                this.instance.Intervals.Length - 1,
                this.instance.BaseOffStateIdx,
                this.instance.Intervals.Last().EnergyCost * this.instance.OffPowerConsumption[this.instance.BaseOffStateIdx]);

            return graph;
        }

        private void AddEdge(
            LayeredGraph graph,
            int fromIntervalIdx,
            int fromStateIdx,
            int toStateIdx)
        {
            int fromRow = fromIntervalIdx;
            
            if (this.instance.StateDiagramTime[fromStateIdx][toStateIdx].HasValue == false)
            {
                // No valid transition from one state to another.
                return;
            }

            // Transition to the next interval if from state is the same as to state (similarly with power consumption).
            int transitionTime = fromStateIdx == toStateIdx ?
                1
                : this.instance.StateDiagramTime[fromStateIdx][toStateIdx].Value;
                
            var toRow = fromRow + transitionTime;

            if (toRow >= graph.RowsCount)
            {
                // Transition outside of the intervals range.
                return;
            }

            int powerConsumption = fromStateIdx == toStateIdx
                ? this.instance.StatePowerConsumption[fromStateIdx]
                : this.instance.StateDiagramPowerConsumption[fromStateIdx][toStateIdx].Value;

            var weight = this.instance.TotalEnergyCost(
                fromIntervalIdx,
                fromIntervalIdx + transitionTime - 1,
                powerConsumption);
            
            graph.AddEdge(fromRow, fromStateIdx, toRow, toStateIdx, weight);
        }
        

        public bool TimeLimitReached
        {
            get
            {
                return this.timer.TimeLimitReached;
            }
        }

        public int?[][] OptimalCosts { get; private set; }
        public int?[][] FullOptimalCosts { get; private set; }
        public List<int>[][] OptimalStates { get; private set; }
    }
}
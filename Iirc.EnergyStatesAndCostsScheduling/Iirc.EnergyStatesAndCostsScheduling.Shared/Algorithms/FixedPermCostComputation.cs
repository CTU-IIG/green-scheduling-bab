// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.DataStructs;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Interface;
    using Iirc.Utils.Collections;
    using Iirc.Utils.Graph;
    using Iirc.Utils.SolverFoundations;
    using Iirc.Utils.Time;

    public class FixedPermCostComputation : IAlgorithm, IHasStartTimes, IHasObjective
    {
        private ExtendedInstance instance;

        private Timer timer;
        private IList<Job> orderedJobs;
        private LayeredGraph graph;
        private int[] cumulativeProcessingTimes;

        public FixedPermCostComputation(ExtendedInstance instance)
        {
            this.instance = instance;
            this.graph = new LayeredGraph(
                this.instance.Intervals.Length,
                this.instance.Jobs.Length * 2);
        }
        
        public void SetInput(IList<Job> orderedJobs)
        {
            this.orderedJobs = orderedJobs;
            this.cumulativeProcessingTimes = new int[orderedJobs.Count];
            var cumulativeProcessingTime = orderedJobs.Sum(job => job.ProcessingTime);
            for (int jobPosition = 0; jobPosition < orderedJobs.Count; jobPosition++)
            {
                this.cumulativeProcessingTimes[jobPosition] = cumulativeProcessingTime;
                cumulativeProcessingTime -= orderedJobs[jobPosition].ProcessingTime;
            }
        }

        public Status Solve(TimeSpan? timeLimit = null)
        {
            this.timer = new Timer(timeLimit);
            this.timer.Start();

            this.Clear();
            
            // Edges from source.
            foreach (var interval in this.instance.Intervals.SkipFirst())
            {
                if (!this.instance.OptimalSwitchingCosts.First()[interval.Index].HasValue)
                {
                    continue;
                }
                
                this.graph.AddEdgeFromSource(
                    0, 
                    interval.Index, 
                    this.instance.OptimalSwitchingCosts.First()[interval.Index].Value);
            }

            for (var jobPosition = 0; jobPosition < this.orderedJobs.Count; jobPosition++)
            {
                if (this.TimeLimitReached)
                {
                    this.timer.Stop();
                    return Status.NoSolution;
                }
                
                var job = this.orderedJobs[jobPosition];
                
                // Start layer.
                foreach (var startInterval in this.instance.Intervals)
                {
                    if (!this.graph.HasIncomingEdge(jobPosition * 2, startInterval.Index))
                    {
                        continue;
                    }

                    // Do not generate further edges if the remaining jobs cannot be scheduled.
                    if ((startInterval.Index + this.cumulativeProcessingTimes[jobPosition] - 1) >
                        this.instance.LatestOnIntervalIdx)
                    {
                        break;
                    }

                    var compIntervalIdx = startInterval.Index + job.ProcessingTime;
                    if (compIntervalIdx > this.instance.Intervals.Last().Index)
                    {
                        continue;
                    }
                    
                    this.graph.AddEdge(
                        jobPosition * 2, 
                        startInterval.Index, 
                        jobPosition * 2 + 1, 
                        compIntervalIdx, 
                        this.instance.TotalEnergyCost(
                            startInterval.Index,
                            compIntervalIdx - 1,
                            this.instance.OnPowerConsumption));
                }
                
                // Completion layer.
                foreach (var compInterval in this.instance.Intervals)
                {
                    if (!this.graph.HasIncomingEdge(jobPosition * 2 + 1, compInterval.Index))
                    {
                        continue;
                    }

                    if (jobPosition == (this.orderedJobs.Count - 1))
                    {
                        // Last job in the order: only edge to sink.
                        if (!this.instance.OptimalSwitchingCosts[compInterval.Index].Last().HasValue)
                        {
                            continue;
                        }
                        
                        this.graph.AddEdgeToSink(
                            jobPosition * 2 + 1,
                            compInterval.Index, 
                            this.instance.OptimalSwitchingCosts[compInterval.Index].Last().Value);
                    }
                    else
                    {
                        for (var startIntervalIdx = compInterval.Index;
                            startIntervalIdx <= this.instance.Intervals.Last().Index;
                            startIntervalIdx++)
                        {
                            if (!this.instance.OptimalSwitchingCosts[compInterval.Index][startIntervalIdx].HasValue)
                            {
                                continue;
                            }

                            this.graph.AddEdge(
                                jobPosition * 2 + 1,
                                compInterval.Index,
                                (jobPosition + 1) * 2,
                                startIntervalIdx,
                                this.instance.OptimalSwitchingCosts[compInterval.Index][startIntervalIdx].Value);
                        }
                    }
                }
            }

            var shortestPaths = new ShortestPaths();
            shortestPaths.SetInput(this.graph, true);
            var shortestPathsStatus = shortestPaths.Solve(this.timer.RemainingTime);
            if (shortestPathsStatus.IsFeasibleSolution())
            {
                this.Objective = shortestPaths.ShortestPathWeight;
                this.ReconstructStartTimes(shortestPaths);
            }
            
            this.timer.Stop();
            return this.TimeLimitReached ? Status.NoSolution : shortestPathsStatus;
        }

        private void Clear()
        {
            this.graph.RemoveEdges();
            this.StartTimes = null;
            this.Objective = null;
        }

        private void ReconstructStartTimes(ShortestPaths shortestPaths)
        {
            this.StartTimes = new StartTimes();

            for (var jobPosition = 0; jobPosition < this.orderedJobs.Count; jobPosition++)
            {
                var job = this.orderedJobs[jobPosition];
                var startNode = shortestPaths.ShortestPath[1 + jobPosition * 2];
                this.StartTimes[job] = this.graph.NodeCol(startNode);
            }
        }

        public StartTimes StartTimes { get; private set; }
        
        public int? Objective { get; private set; }
        
        public bool TimeLimitReached
        {
            get
            {
                return this.timer.TimeLimitReached;
            }
        }
    }
}

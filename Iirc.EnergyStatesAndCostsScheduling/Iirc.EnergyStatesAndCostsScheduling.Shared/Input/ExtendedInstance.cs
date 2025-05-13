// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Input
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Algorithms;

    [Serializable]
    public class ExtendedInstance : Instance
    {
        public ExtendedInstance(Instance instance)
            : base(
                machinesCount: instance.MachinesCount,
                jobs: instance.Jobs,
                intervals: instance.Intervals,
                lengthInterval: instance.LengthInterval,
                offOnTime: instance.OffOnTime,
                onOffTime: instance.OnOffTime,
                offOnPowerConsumption: instance.OffOnPowerConsumption,
                onOffPowerConsumption: instance.OnOffPowerConsumption,
                offIdleTime: instance.OffIdleTime,
                idleOffTime: instance.IdleOffTime,
                offIdlePowerConsumption: instance.OffIdlePowerConsumption,
                idleOffPowerConsumption: instance.IdleOffPowerConsumption,
                onPowerConsumption: instance.OnPowerConsumption,
                idlePowerConsumption: instance.IdlePowerConsumption,
                offPowerConsumption: instance.OffPowerConsumption,
                metadata: instance.Metadata)
        {
            this.Machines = Enumerable.Range(0, instance.MachinesCount).ToList();

            this.MachineJobs =
                Enumerable.Repeat(0, instance.MachinesCount).Select(_ => new List<Job>()).ToList();
            foreach (var job in this.Jobs)
            {
                this.MachineJobs[job.MachineIdx].Add(job);
            }
            
            this.InitStateDiagram();
            
            // TODO: should be computed using shortest paths between states.
            this.EarliestOnIntervalIdx = 1 + this.OffOnTime[this.BaseOffStateIdx];
            this.LatestOnIntervalIdx = this.Intervals.Length - (this.OnOffTime[this.BaseOffStateIdx] + 1) - 1;
        }

        private void InitStateDiagram()
        {
            this.OffStateInds = Enumerable.Range(0, OffPowerConsumption.Length).ToList();
            this.BaseOffStateIdx = this.OffStateInds[0];
            this.OnStateIdx = this.OffStateInds.Count;
            this.IdleStateIdx = this.OnStateIdx + 1;

            this.States = Enumerable
                .Repeat(StateKind.Off, this.OffStateInds.Count)
                .Concat(new List<StateKind> {StateKind.On, StateKind.Idle})
                .ToArray();

            this.StateDiagramPowerConsumption = Enumerable
                .Repeat(0, this.States.Length)
                .Select(_ => new int?[this.States.Length])
                .ToArray();
            this.StateDiagramTime = Enumerable
                .Repeat(0, this.States.Length)
                .Select(_ => new int?[this.States.Length])
                .ToArray();

            foreach (var offState in this.OffStateInds)
            {
                this.StateDiagramPowerConsumption[offState][this.OnStateIdx] = this.OffOnPowerConsumption[offState];
                this.StateDiagramPowerConsumption[this.OnStateIdx][offState] = this.OnOffPowerConsumption[offState];
                this.StateDiagramTime[offState][this.OnStateIdx] = this.OffOnTime[offState];
                this.StateDiagramTime[this.OnStateIdx][offState] = this.OnOffTime[offState];
                
                this.StateDiagramPowerConsumption[offState][this.IdleStateIdx] = this.OffIdlePowerConsumption[offState];
                this.StateDiagramPowerConsumption[this.IdleStateIdx][offState] = this.IdleOffPowerConsumption[offState];
                this.StateDiagramTime[offState][this.IdleStateIdx] = this.OffIdleTime[offState];
                this.StateDiagramTime[this.IdleStateIdx][offState] = this.IdleOffTime[offState];
            }

            // Transition between On and Idle.
            this.StateDiagramPowerConsumption[this.IdleStateIdx][this.OnStateIdx] = 0;
            this.StateDiagramPowerConsumption[this.OnStateIdx][this.IdleStateIdx] = 0;
            this.StateDiagramTime[this.IdleStateIdx][this.OnStateIdx] = 0;
            this.StateDiagramTime[this.OnStateIdx][this.IdleStateIdx] = 0;
            
            // Remaining in state: transition with time 0 and 0 power consumption.
            foreach (var stateIdx in Enumerable.Range(0, this.States.Length))
            {
                this.StateDiagramPowerConsumption[stateIdx][stateIdx] = 0;
                this.StateDiagramTime[stateIdx][stateIdx] = 0;
            }
            
            this.StatePowerConsumption = this.OffPowerConsumption
                .Concat(new [] { this.OnPowerConsumption, this.IdlePowerConsumption })
                .ToArray();

            this.StateInds = Enumerable.Range(0, this.States.Length).ToList();
            
            this.ComputeCumulativeEnergyCost();
        }

        public void ComputeOptimalSwitchingCosts()
        {
            if (this.OptimalSwitchingCosts != null)
            {
                return;
            }
            
            var switchingSolver = new ShortestPathAlgorithmCostEfficientSwitchings(this);
            switchingSolver.Solve(null);
            this.OptimalSwitchingCosts = switchingSolver.OptimalCosts;
            this.FullOptimalSwitchingCosts = switchingSolver.FullOptimalCosts;
        }

        public void ComputeGapsLowerBounds()
        {
            if (this.GapsLowerBounds != null)
            {
                return;
            }
            
            var gapsSolver = new LowerBoundsOnSpaces(this);
            gapsSolver.Solve(null);
            this.GapsLowerBounds = gapsSolver.GapsLowerBounds;
        }

        public void GenerateFullExtendedInstance()
        {
            this.ComputeOptimalSwitchingCosts();
            this.ComputeGapsLowerBounds();
        }

        private void ComputeCumulativeEnergyCost()
        {
            this.CumulativeEnergyCost = Enumerable
                .Repeat(0, this.Intervals.Length)
                .Select(_ => new int[this.Intervals.Length])
                .ToArray();

            for (var fromIntervalIdx = 0; fromIntervalIdx < this.Intervals.Length; fromIntervalIdx++)
            {
                var cumulativeEnergyCost = 0;
                for (var toIntervalIdx = fromIntervalIdx; toIntervalIdx < this.Intervals.Length; toIntervalIdx++)
                {
                    cumulativeEnergyCost += this.Intervals[toIntervalIdx].EnergyCost;
                    this.CumulativeEnergyCost[fromIntervalIdx][toIntervalIdx] = cumulativeEnergyCost;
                }
            }
        }

        public static ExtendedInstance GetExtendedInstance(Instance instance)
        {
            if (instance is ExtendedInstance)
            {
                return (ExtendedInstance) instance;
            }

            if (instance.SerializedExtendedInstance != null)
            {
                var stream = new MemoryStream(instance.SerializedExtendedInstance);
                using (stream)
                {
                    var bf = new BinaryFormatter();
                    var extendedInstance = (ExtendedInstance)bf.Deserialize(stream);
                    extendedInstance.Metadata = instance.Metadata;    // Due to metadata possibly not serializable.
                    return extendedInstance;
                }
            }
            
            return new ExtendedInstance(instance);
        }
        
        /// <summary>
        /// Gets the machine indices.
        /// </summary>
        public IReadOnlyList<int> Machines { get; private set; }
        
        /// <summary>
        /// Gets jobs dedicated to each machine.
        /// </summary>
        public IReadOnlyList<List<Job>> MachineJobs { get; private set; }
        
        /// <summary>
        /// Gets the optimal switching costs (w.r.t. TEC) indexed by the intervals. The first index is the earlier
        /// interval (inclusive), the second is the latest interval (exclusive). If no switching is possible, then the
        /// value is null. The length of each dimension is number of intervals + 1 to support zero time transitions
        /// from on to base off at the end of the horizon.
        /// </summary>
        /// <remarks>
        /// For example, if we have 10 intervals and earliest interval is 3 and latest interval is 5, then the switching
        /// cost represent the optimal TEC of states in intervals 3, 4 while assuming that in intervals 2 and 5 the
        /// machine is in on state. It is assumed that in the first interval and the last the machines are in the base
        /// off state.
        /// </remarks>
        public int?[][] OptimalSwitchingCosts { get; private set; }
        
        /// <summary>
        /// Gets the full optimal switching costs. The difference from <see cref="OptimalSwitchingCosts"/> is that
        /// <see cref="FullOptimalSwitchingCosts"/> has all the costs, whereas <see cref="OptimalSwitchingCosts"/>
        /// may pruned some of them if deemed unimportant.
        /// </summary>
        public int?[][] FullOptimalSwitchingCosts { get; private set; }
        
        
        /// <summary>
        /// Gets the lower bounds on the costs of potential gaps (w.r.t. TEC) indexed by the intervals.
        /// The first index is the earlier interval (inclusive), the second is the latest interval (exclusive);
        /// Default value is 0 (e.g., when the gap is not even feasible)
        /// </summary>
        public int?[][] GapsLowerBounds { get; private set; }
        
        
        /// <summary>
        /// Gets the index of the idle state into <see cref="States"/>.
        /// </summary>
        public int IdleStateIdx { get; private set; }
        
        /// <summary>
        /// Gets the index of the on state into <see cref="States"/>.
        /// </summary>
        public int OnStateIdx { get; private set; }
        
        /// <summary>
        /// Gets the index of the base off state into <see cref="States"/>.
        /// </summary>
        public int BaseOffStateIdx { get; private set; }
        
        /// <summary>
        /// Gets the indices of the off states into <see cref="States"/>.
        /// </summary>
        public List<int> OffStateInds { get; private set; }
        
        /// <summary>
        /// Gets the states: off 0 (base off), off 1, off 2, ..., off n, on, idle
        /// </summary>
        public StateKind[] States { get; private set; }
        
        /// <summary>
        /// Gets the indices of the states.
        /// </summary>
        public List<int> StateInds { get; private set; }
        
        /// <summary>
        /// Gets the power consumption of transition from one state to another. If the transition is forbidden, the
        /// value is null.
        /// </summary>
        public int?[][] StateDiagramPowerConsumption { get; private set; }
        
        /// <summary>
        /// Gets the transition time from one state to another. If the transition is forbidden, the value is null.
        /// </summary>
        public int?[][] StateDiagramTime { get; private set; }
        
        /// <summary>
        /// Gets the power consumption of each state.
        /// </summary>
        public int[] StatePowerConsumption { get; private set; }
        
        /// <summary>
        /// Gets the earliest interval which can be in on state.
        /// </summary>
        public int EarliestOnIntervalIdx { get; private set; }
        
        /// <summary>
        /// Gets the latest interval which can be in on state.
        /// </summary>
        public int LatestOnIntervalIdx { get; private set; }
        
        /// <summary>
        /// Gets the cumulative energy cost of intervals [fromIntervalIdx, ..., toIntervalIdx] .
        /// </summary>
        public int[][] CumulativeEnergyCost { get; private set; }
    }
    
    /// <summary>
    /// What kind each state is.
    /// </summary>
    public enum StateKind
    {
        Off = 0,
        On = 1,
        Idle = 2
    }
}

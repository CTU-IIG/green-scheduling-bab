// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Input
{
    using System;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Interface;
    using Iirc.Utils.SolverFoundations;

    /// <summary>
    /// The instance of the scheduling problem.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The times (transition, processing, etc.) are NOT relative to the intervals length, they are absolute.
    /// For example, if <see cref="LengthInterval"/> is 15 and <see cref="Job.ProcessingTime"/> is 30, then it takes
    /// two intervals to process the job (the same applies for transition times).
    /// </para>
    /// <para>
    /// The base off state is the one in which the machines must be at the beginning and end of the scheduling horizon
    /// (but not necessary during the first and last metering interval). The base off state has zero index.
    /// </para>
    /// </remarks>
    [Serializable]
    public class Instance : IInstance
    {
        /// <summary>
        /// Gets the number of machines. Note that currently only single machine instances are supported.
        /// </summary>
        public int MachinesCount { get; }
        
        /// <summary>
        /// Gets the jobs.
        /// </summary>
        public Job[] Jobs { get; }
        
        /// <summary>
        /// Gets the intervals.
        /// </summary>
        public Interval[] Intervals { get; }
        
        /// <summary>
        /// Gets the length of the intervals. Note that currently only intervals of unit length are supported.
        /// </summary>
        public int LengthInterval { get; }
        
        /// <summary>
        /// Gets the transition time from off states to on state.
        /// </summary>
        public int[] OffOnTime { get; }
        
        /// <summary>
        /// Gets the transition time from on state to off states.
        /// </summary>
        public int[] OnOffTime { get; }
        
        /// <summary>
        /// Gets the power consumption of transition from off states to on state.
        /// </summary>
        public int[] OffOnPowerConsumption { get; }
        
        /// <summary>
        /// Gets the power consumption of transition from on state to off states.
        /// </summary>
        public int[] OnOffPowerConsumption { get; }
        
        /// <summary>
        /// Gets the transition time from off states to idle state. If the transition is forbidden, the corresponding
        /// value is null.
        /// </summary>
        public int?[] OffIdleTime { get; }
        
        /// <summary>
        /// Gets the transition time from idle state to off states. If the transition is forbidden, the corresponding
        /// value is null.
        /// </summary>
        public int?[] IdleOffTime { get; }
        
        /// <summary>
        /// Gets the power consumption of transition from off states to idle state. If the transition is forbidden, the
        /// corresponding value is null.
        /// </summary>
        public int?[] OffIdlePowerConsumption { get; }
        
        /// <summary>
        /// Gets the power consumption of transition from idle state to off states. If the transition is forbidden, the
        /// corresponding value is null.
        /// </summary>
        public int?[] IdleOffPowerConsumption { get; }
        
        /// <summary>
        /// Gets the power consumption of the on state.
        /// </summary>
        public int OnPowerConsumption { get; }
        
        /// <summary>
        /// Gets the power consumption of the idle state.
        /// </summary>
        public int IdlePowerConsumption { get; }
        
        /// <summary>
        /// Gets the power consumption of the off states.
        /// </summary>
        public int[] OffPowerConsumption { get; }
        
        /// <summary>
        /// Gets the metadata of the instance.
        /// </summary>
        public object Metadata { get; set; }
        
        /// <summary>
        /// Gets the serialized extended instance.
        /// </summary>
        public byte[] SerializedExtendedInstance { get; set; }
        
        /// <summary>
        /// Gets the time needed for creating the extended instance.
        /// </summary>
        public TimeSpan? TimeForExtendedInstance { get; set; }

        public Instance(
            int machinesCount,
            Job[] jobs,
            Interval[] intervals,
            int lengthInterval,
            int[] offOnTime,
            int[] onOffTime,
            int[] offOnPowerConsumption,
            int[] onOffPowerConsumption,
            int?[] offIdleTime,
            int?[] idleOffTime,
            int?[] offIdlePowerConsumption,
            int?[] idleOffPowerConsumption,
            int onPowerConsumption,
            int idlePowerConsumption,
            int[] offPowerConsumption,
            object metadata = null,
            byte[] serializedExtendedInstance = null,
            TimeSpan? timeForExtendedInstance = null)
        {
            this.MachinesCount = machinesCount;
            this.Jobs = jobs;
            this.Intervals = intervals;
            this.LengthInterval = lengthInterval;
            this.OffOnTime = offOnTime;
            this.OnOffTime = onOffTime;
            this.OffOnPowerConsumption = offOnPowerConsumption;
            this.OnOffPowerConsumption = onOffPowerConsumption;
            this.OffIdleTime = offIdleTime;
            this.IdleOffTime = idleOffTime;
            this.OffIdlePowerConsumption = offIdlePowerConsumption;
            this.IdleOffPowerConsumption = idleOffPowerConsumption;
            this.OnPowerConsumption = onPowerConsumption;
            this.IdlePowerConsumption = idlePowerConsumption;
            this.OffPowerConsumption = offPowerConsumption;
            this.Metadata = metadata;
            this.SerializedExtendedInstance = serializedExtendedInstance;
            this.TimeForExtendedInstance = timeForExtendedInstance;
        }

        public Instance(
            int machinesCount,
            Job[] jobs,
            Interval[] intervals,
            int lengthInterval,
            IStateDiagram stateDiagram,
            object metadata = null,
            byte[] serializedExtendedInstance = null,
            TimeSpan? timeForExtendedInstance = null)
            : this(
                machinesCount: machinesCount,
                jobs: jobs,
                intervals: intervals,
                lengthInterval: lengthInterval,
                offOnTime: stateDiagram.OffOnTime,
                onOffTime: stateDiagram.OnOffTime,
                offOnPowerConsumption: stateDiagram.OffOnPowerConsumption,
                onOffPowerConsumption: stateDiagram.OnOffPowerConsumption,
                offIdleTime: stateDiagram.OffIdleTime,
                idleOffTime: stateDiagram.IdleOffTime,
                offIdlePowerConsumption: stateDiagram.OffIdlePowerConsumption,
                idleOffPowerConsumption: stateDiagram.IdleOffPowerConsumption,
                onPowerConsumption: stateDiagram.OnPowerConsumption,
                idlePowerConsumption: stateDiagram.IdlePowerConsumption,
                offPowerConsumption: stateDiagram.OffPowerConsumption,
                metadata: metadata,
                serializedExtendedInstance: serializedExtendedInstance,
                timeForExtendedInstance: timeForExtendedInstance)
        {
        }
    }
}
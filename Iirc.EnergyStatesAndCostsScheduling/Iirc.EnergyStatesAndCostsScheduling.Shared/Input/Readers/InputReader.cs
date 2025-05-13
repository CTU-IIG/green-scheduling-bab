// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Input.Readers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Interface.Input;
    using Newtonsoft.Json;

    public class InputReader : IInputReader
    {
        public Instance ReadFromPath(string instancePath)
        {
            var jsonInstance = JsonConvert.DeserializeObject<JsonInstance>(File.ReadAllText(instancePath));

            var jobs = jsonInstance.Jobs
                .Select((jsonJob, jobIndex) => new Job(
                    id: jsonJob.Id,
                    index: jobIndex,
                    machineIdx: jsonJob.MachineIdx,
                    processingTime: jsonJob.ProcessingTime))
                .ToArray();

            var intervals = new List<Interval>();
            for (var intervalIndex = 0; intervalIndex < jsonInstance.EnergyCosts.Length; intervalIndex++)
            {
                var start = intervalIndex * jsonInstance.LengthInterval;
                var end = start + jsonInstance.LengthInterval;
                intervals.Add(new Interval(
                    index: intervalIndex,
                    start: start,
                    end: end,
                    energyCost: jsonInstance.EnergyCosts[intervalIndex]));
            }

            return new Instance(
                machinesCount: jsonInstance.MachinesCount,
                jobs: jobs,
                intervals: intervals.ToArray(),
                lengthInterval: jsonInstance.LengthInterval,
                offOnTime: jsonInstance.OffOnTime,
                onOffTime: jsonInstance.OnOffTime,
                offOnPowerConsumption: jsonInstance.OffOnPowerConsumption,
                onOffPowerConsumption: jsonInstance.OnOffPowerConsumption,
                offIdleTime: jsonInstance.OffIdleTime,
                idleOffTime: jsonInstance.IdleOffTime,
                offIdlePowerConsumption: jsonInstance.OffIdlePowerConsumption,
                idleOffPowerConsumption: jsonInstance.IdleOffPowerConsumption,
                onPowerConsumption: jsonInstance.OnPowerConsumption,
                idlePowerConsumption: jsonInstance.IdlePowerConsumption,
                offPowerConsumption: jsonInstance.OffPowerConsumption,
                serializedExtendedInstance: jsonInstance.SerializedExtendedInstance,
                metadata: jsonInstance.Metadata,
                timeForExtendedInstance: jsonInstance.TimeForExtendedInstance
            );
        }
    }

    /// <summary>
    /// The job.
    /// </summary>
    public class JsonJob
    {
        /// <summary>
        /// Gets or sets the unique id of job within an instance.
        /// </summary>
        public int Id { get; set; }
        
        /// <summary>
        /// Gets or sets the index of the machine dedicated to this job.
        /// </summary>
        public int MachineIdx { get; set; }
        
        /// <summary>
        /// Gets or sets the processing time.
        /// </summary>
        public int ProcessingTime { get; set; }
    }
    
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
    public class JsonInstance
    {
        /// <summary>
        /// Gets or sets the number of machines. Note that currently only single machine instances are supported.
        /// </summary>
        public int MachinesCount { get; set; }
        
        /// <summary>
        /// Gets or sets the jobs.
        /// </summary>
        public JsonJob[] Jobs { get; set; }
        
        /// <summary>
        /// Gets or sets the energy costs in the intervals.
        /// </summary>
        public int[] EnergyCosts { get; set; }
        
        /// <summary>
        /// Gets or sets the length of the intervals. Note that currently only intervals of unit length are supported.
        /// </summary>
        public int LengthInterval { get; set; }
        
        /// <summary>
        /// Gets or sets the transition time from off states to on state.
        /// </summary>
        public int[] OffOnTime { get; set; }
        
        /// <summary>
        /// Gets or sets the transition time from on state to off states.
        /// </summary>
        public int[] OnOffTime { get; set; }
        
        /// <summary>
        /// Gets or sets the power consumption of transition from off states to on state.
        /// </summary>
        public int[] OffOnPowerConsumption { get; set; }
        
        /// <summary>
        /// Gets or sets the power consumption of transition from on state to off states.
        /// </summary>
        public int[] OnOffPowerConsumption { get; set; }
        
        /// <summary>
        /// Gets or sets the transition time from off states to idle state. If the transition is forbidden, the
        /// corresponding value is null.
        /// </summary>
        public int?[] OffIdleTime { get; set; }
        
        /// <summary>
        /// Gets or sets the transition time from idle state to off states. If the transition is forbidden, the
        /// corresponding value is null.
        /// </summary>
        public int?[] IdleOffTime { get; set; }
        
        /// <summary>
        /// Gets or sets the power consumption of transition from off states to idle state. If the transition is
        /// forbidden, the corresponding value is null.
        /// </summary>
        public int?[] OffIdlePowerConsumption { get; set; }
        
        /// <summary>
        /// Gets or sets the power consumption of transition from idle state to off states. If the transition is
        /// forbidden, the corresponding value is null.
        /// </summary>
        public int?[] IdleOffPowerConsumption { get; set; }
        
        /// <summary>
        /// Gets or sets the power consumption of the on state.
        /// </summary>
        public int OnPowerConsumption { get; set; }
        
        /// <summary>
        /// Gets or sets the power consumption of the idle state.
        /// </summary>
        public int IdlePowerConsumption { get; set; }
        
        /// <summary>
        /// Gets or sets the power consumption of the off states.
        /// </summary>
        public int[] OffPowerConsumption { get; set; }
        
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
    }
}
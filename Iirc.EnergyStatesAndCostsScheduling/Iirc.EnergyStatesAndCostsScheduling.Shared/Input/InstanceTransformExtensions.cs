// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Input
{
    using System.Collections.Generic;
    using System.Linq;
    using Iirc.Utils.Collections;

    public static class InstanceTransformExtensions
    {
        public static Instance OnlyFromJobs(this Instance instance, IEnumerable<Job> jobs)
        {
            var newJobs = jobs
                .Select((job, newJobIndex) => new Job(
                    job.Id,
                    newJobIndex,
                    job.MachineIdx,
                    job.ProcessingTime))
                .ToArray();

            return new Instance(
                machinesCount: instance.MachinesCount,
                jobs: newJobs,
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
                metadata: instance.Metadata
            );
        }

        public static Instance UnitProcTime(this ExtendedInstance instance)
        {
            var newJobId = 0;
            var newJobs = new List<Job>();
            foreach (var machineIdx in instance.Machines)
            {
                int totalProcessingTimeOnMachine = instance.MachineJobs[machineIdx].Sum(job => job.ProcessingTime);
                for (int unit = 0; unit < totalProcessingTimeOnMachine; unit++)
                {
                    newJobs.Add(new Job(newJobId, newJobId, machineIdx, 1));
                    newJobId++;
                }
            }
            return new Instance(
                machinesCount: instance.MachinesCount,
                jobs: newJobs.ToArray(),
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
                metadata: instance.Metadata
            );
        }
    }
}
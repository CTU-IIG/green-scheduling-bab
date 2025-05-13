// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Input
{
    public class InstanceChecker
    {
        private Instance instance;

        public InstanceStatus Status { get; private set; }
        
        public Interval Interval { get; private set; }
        public Job Job { get; private set; }

        public InstanceStatus Check(Instance instance)
        {
            this.instance = instance;

            var ok = true;
            this.Status = InstanceStatus.Unknown;

            if (ok)
            {
                ok &=
                    this.ValidMachineIndicesInJobs()
                    && this.SingleMachineInstance()
                    && this.ProcessingTimesArePositive()
                    && this.EnergyCostsAreNonNegative()
                    && this.IntervalsHaveUnitLength();
            }

            if (ok)
            {
                this.Status = InstanceStatus.Ok;
            }

            return this.Status;
        }

        private bool ProcessingTimesArePositive()
        {
            foreach (var job in this.instance.Jobs)
            {
                if (job.ProcessingTime <= 0)
                {
                    this.Status = InstanceStatus.NonPositiveProcessingTime;
                    this.Job = job;
                    return false;
                }
            }

            return true;
        }

        private bool EnergyCostsAreNonNegative()
        {
            foreach (var interval in this.instance.Intervals)
            {
                if (interval.EnergyCost < 0)
                {
                    this.Status = InstanceStatus.EnergyCostIsNegative;
                    this.Interval = interval;
                    return false;
                }
            }

            return true;
        }

        private bool ValidMachineIndicesInJobs()
        {
            foreach (var job in this.instance.Jobs)
            {
                if (job.MachineIdx < 0 || job.MachineIdx >= this.instance.MachinesCount)
                {
                    this.Status = InstanceStatus.InvalidMachineIndexForJob;
                    this.Job = job;
                    return false;
                }
            }

            return true;
        }
        
        private bool SingleMachineInstance()
        {
            if (this.instance.MachinesCount != 1)
            {
                this.Status = InstanceStatus.MultipleOrNoMachines;
                return false;
            }

            return true;
        }
        
        private bool IntervalsHaveUnitLength()
        {
            foreach (var interval in this.instance.Intervals)
            {
                if ((interval.End - interval.Start) != 1)
                {
                    this.Status = InstanceStatus.NonUnitLengthInterval;
                    this.Interval = interval;
                    return false;
                }
            }

            return true;
        }

        public enum InstanceStatus
        {
            Unknown = 0,
            Ok = 1,
            EnergyCostIsNegative = 2,
            InvalidMachineIndexForJob = 3,
            NonPositiveProcessingTime = 4,
            NonUnitLengthInterval = 5,
            MultipleOrNoMachines = 6,
        }
    }
}
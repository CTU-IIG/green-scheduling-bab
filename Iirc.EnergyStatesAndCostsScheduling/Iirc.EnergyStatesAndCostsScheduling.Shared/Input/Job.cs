// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Input
{
    using System;

    [Serializable]
    public class Job
    {
        public int Id { get; }
        public int Index { get; }
        public int MachineIdx { get; }
        public int ProcessingTime { get; }

        public Job(int id, int index, int machineIdx, int processingTime)
        {
            this.Id = id;
            this.Index = index;
            this.MachineIdx = machineIdx;
            this.ProcessingTime = processingTime;
        }

        protected bool Equals(Job other)
        {
            return this.Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return this.Equals((Job) obj);
        }

        public override int GetHashCode()
        {
            return this.Id;
        }

        public override string ToString()
        {
            return this.Id.ToString();
        }
    }
}
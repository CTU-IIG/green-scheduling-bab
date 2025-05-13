// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Input
{
    using System;

    [Serializable]
    public class Interval
    {
        public int Index { get; }
        public int Start { get; }
        public int End { get; }
        public int EnergyCost { get; }

        public Interval(
            int index,
            int start,
            int end,
            int energyCost)
        {
            this.Index = index;
            this.Start = start;
            this.End = end;
            this.EnergyCost = energyCost;
        }
        
        public override string ToString()
        {
            return this.Index.ToString();
        }
    }
}

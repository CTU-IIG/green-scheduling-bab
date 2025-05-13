// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.StateDiagrams
{
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Interface;

    /// <summary>
    /// The TWOSBY state diagram described in [Benedikt2020a].
    /// </summary>
    public class Benedikt2020aTwosby : IStateDiagram
    {
        public Benedikt2020aTwosby()
        {
            this.OffOnTime = new [] {4, 3, 2};
            this.OnOffTime = new [] {1, 1, 1};
            this.OffOnPowerConsumption = new [] {15, 13, 12};
            this.OnOffPowerConsumption = new [] {2, 2, 2};
            this.OffIdleTime = new int?[] {null, null, null};
            this.IdleOffTime = new int?[] {null, null, null};
            this.OffIdlePowerConsumption = new int?[] {null, null, null};
            this.IdleOffPowerConsumption = new int?[] {null, null, null};
            this.OnPowerConsumption = 10;
            this.IdlePowerConsumption = 8;
            this.OffPowerConsumption = new [] {0, 2, 4};
        }
        
        public int[] OffOnTime { get; set; }
        public int[] OnOffTime { get; set; }
        public int[] OffOnPowerConsumption { get; set; }
        public int[] OnOffPowerConsumption { get; set; }
        public int?[] OffIdleTime { get; set; }
        public int?[] IdleOffTime { get; set; }
        public int?[] OffIdlePowerConsumption { get; set; }
        public int?[] IdleOffPowerConsumption { get; set; }
        public int OnPowerConsumption { get; set; }
        public int IdlePowerConsumption { get; set; }
        public int[] OffPowerConsumption { get; set; }
    }
}

// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.StateDiagrams
{
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Interface;
    
    /// <summary>
    /// The NOSBY state diagram described in [Benedikt2020a]. Originally proposed in [Aghelinejad2017a].
    /// </summary>
    public class Aghelinejad2017a : IStateDiagram
    {
        public Aghelinejad2017a()
        {
            this.OffOnTime = new [] {2};
            this.OnOffTime = new [] {1};
            this.OffOnPowerConsumption = new [] {5};
            this.OnOffPowerConsumption = new [] {1};
            this.OffIdleTime = new int?[] {null};
            this.IdleOffTime = new int?[] {null};
            this.OffIdlePowerConsumption = new int?[] {null};
            this.IdleOffPowerConsumption = new int?[] {null};
            this.OnPowerConsumption = 4;
            this.IdlePowerConsumption = 2;
            this.OffPowerConsumption = new [] {0};
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

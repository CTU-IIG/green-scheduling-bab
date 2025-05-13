// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Interface
{
    public interface IStateDiagram
    {
        int[] OffOnTime { get; }
        int[] OnOffTime { get; }
        int[] OffOnPowerConsumption { get; }
        int[] OnOffPowerConsumption { get; }
        int?[] OffIdleTime { get; }
        int?[] IdleOffTime { get; }
        int?[] OffIdlePowerConsumption { get; }
        int?[] IdleOffPowerConsumption { get; }
        int OnPowerConsumption { get; }
        int IdlePowerConsumption { get; }
        int[] OffPowerConsumption { get; }
    }
}

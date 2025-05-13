// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Interface.Input
{
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;

    public interface IInputReader
    {
        Instance ReadFromPath(string instancePath);
    }
}
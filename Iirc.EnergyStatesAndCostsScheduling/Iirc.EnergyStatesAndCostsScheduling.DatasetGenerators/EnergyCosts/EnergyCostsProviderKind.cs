// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.EnergyCosts
{
    /// <summary>
    /// The energy costs providers.
    /// </summary>
    public enum EnergyCostsProviderKind
    {
        /// <summary>
        /// See
        /// <see cref="Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.EnergyCosts.UniformEnergyCostsProvider"/>.
        /// </summary>
        Uniform = 0,
            
        /// <summary>
        /// See
        /// <see cref="Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.EnergyCosts.FileEnergyCostsProvider"/>.
        /// </summary>
        File = 1,
    }
}
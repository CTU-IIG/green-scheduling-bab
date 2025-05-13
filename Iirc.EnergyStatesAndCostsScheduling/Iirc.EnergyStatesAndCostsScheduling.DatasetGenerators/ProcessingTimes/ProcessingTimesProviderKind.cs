// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.ProcessingTimes
{
    /// <summary>
    /// The processing times providers.
    /// </summary>
    public enum ProcessingTimesProviderKind
    {
        /// <summary>
        /// See
        /// <see cref="Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.ProcessingTimes.UniformProcessingTimesProvider"/>.
        /// </summary>
        Uniform = 0,
        
        /// <summary>
        /// See
        /// <see cref="Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.ProcessingTimes.GroupsProcessingTimesProvider"/>.
        /// </summary>
        Groups = 1,
    }
}
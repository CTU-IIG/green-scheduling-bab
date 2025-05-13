// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.Interface
{
    using System.Collections.Generic;

    /// <summary>
    /// The interface for processing times provider.
    /// </summary>
    public interface IProcessingTimesProvider
    {
        /// <summary>
        /// Generates next processing time.
        /// </summary>
        /// <returns>The processing time.</returns>
        int Generate();
        
        Dictionary<string, object> GetConfig();
    }
}

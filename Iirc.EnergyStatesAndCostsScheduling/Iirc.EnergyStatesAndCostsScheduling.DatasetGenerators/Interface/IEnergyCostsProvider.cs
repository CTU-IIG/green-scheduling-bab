// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.Interface
{
    using System.Collections.Generic;

    /// <summary>
    /// The interface for energy costs provider.
    /// </summary>
    public interface IEnergyCostsProvider
    {
        /// <summary>
        /// Generates <paramref name="intervalsCount"/> costs.
        /// </summary>
        /// <param name="intervalsCount">The number of costs to generate.</param>
        /// <param name="costRepeatCount">The number of consecutive costs that are the same. Useful for changing the
        /// granularity of the scheduling horizon (e.g., for hourly RTP pricing and 1 minute scheduling granularity, set
        /// <paramref name="costRepeatCount"/> to 60).</param>
        /// <returns>The generated costs.</returns>
        List<int> Generate(int intervalsCount, int costRepeatCount);
        
        Dictionary<string, object> GetConfig();
    }
}

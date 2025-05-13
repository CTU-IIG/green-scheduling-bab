// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.EnergyCosts
{
    using System;
    using System.Collections.Generic;
    using Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.Interface;

    /// <summary>
    /// Provider for the energy costs that samples the cost from a discrete uniform distribution
    /// [minEnergyCost, maxEnergyCost].
    /// </summary>
    public class UniformEnergyCostsProvider : IEnergyCostsProvider
    {
        public UniformEnergyCostsProvider(int minEnergyCost, int maxEnergyCost, Random rnd)
        {
            this.MinEnergyCost = minEnergyCost;
            this.MaxEnergyCost = maxEnergyCost;
            this.Rnd = rnd;
        }
        
        public static IEnergyCostsProvider FromConfig(Config config, Random rnd)
        {
            return new UniformEnergyCostsProvider(config.MinEnergyCost, config.MaxEnergyCost, rnd);
        }
        
        protected int MinEnergyCost { get; set; }
        protected int MaxEnergyCost { get; set; }
        protected Random Rnd { get; set; }

        public List<int> Generate(int intervalsCount, int costRepeatCount)
        {
            var costs = new List<int>();

            while (true)
            {
                var cost = this.Rnd.Next(this.MinEnergyCost, this.MaxEnergyCost + 1);
                for (int i = 0; i < costRepeatCount; i++)
                {
                    if (costs.Count >= intervalsCount)
                    {
                        return costs;
                    }
                    
                    costs.Add(cost);
                }
            }
        }
        
        public Dictionary<string, object> GetConfig()
        {
            return new Dictionary<string, object>
            {
                {"MinEnergyCost", this.MinEnergyCost},
                {"MaxEnergyCost", this.MaxEnergyCost},
            };
        }
        
        public class Config
        {
            /// <summary>
            /// Gets or sets the minimum energy cost (inclusive).
            /// </summary>
            public int MinEnergyCost { get; set; }
            
            /// <summary>
            /// Gets or sets the maximum energy cost (inclusive).
            /// </summary>
            public int MaxEnergyCost { get; set; }
        }
    }
}

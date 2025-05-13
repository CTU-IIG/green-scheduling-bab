// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.ProcessingTimes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.Interface;
    using Iirc.Utils.Math;

    /// <summary>
    /// Provider for the processing times that samples the processing times from the given groups according to the
    /// given probabilities.
    /// </summary>
    public class GroupsProcessingTimesProvider : IProcessingTimesProvider
    {
        public GroupsProcessingTimesProvider(List<int> processingTimes, List<double> probabilities, Random rnd)
        {
            this.ProcessingTimes = processingTimes;
            this.Probabilities = probabilities;
            this.Rnd = rnd;

            if (this.Probabilities != null)
            {
                if (!NumericComparer.Default.AreEqual(this.Probabilities.Sum(), 1.0))
                {
                    throw new ArgumentException("Probabilities must sum to 1.0!");
                }
            }
        }
        
        public static IProcessingTimesProvider FromConfig(Config config, Random rnd)
        {
            return new GroupsProcessingTimesProvider(config.ProcessingTimes, config.Probabilities, rnd);
        }

        protected List<int> ProcessingTimes { get; set; }
        protected List<double> Probabilities { get; set; }

        protected Random Rnd { get; set; }
        
        public int Generate()
        {
            if (this.Probabilities == null)
            {
                // Uniform probability.
                return this.ProcessingTimes[this.Rnd.Next(0, this.ProcessingTimes.Count)];
            }
            else
            {
                double randVal = this.Rnd.NextDouble();
                double cumulProbability = 0.0;
                for (int i = 0; i < ProcessingTimes.Count; i++)
                {
                    cumulProbability += this.Probabilities[i];
                    if (randVal <= cumulProbability)
                    {
                        return this.ProcessingTimes[i];
                    }
                }
                
                // If we get here, probably some numeric comparison issue.
                return this.ProcessingTimes.Last();
            }
        }

        public Dictionary<string, object> GetConfig()
        {
            return new Dictionary<string, object>
            {
                {"ProcessingTimes", this.ProcessingTimes},
                {"Probabilities", this.Probabilities},
            };
        }
        
        public class Config
        {
            /// <summary>
            /// Gets or sets a processing time of each group.
            /// </summary>
            public List<int> ProcessingTimes { get; set; }
            
            /// <summary>
            /// Gets or sets the sampling probabilities of the group processing times. 
            /// If null, then uniform sampling probability is assumed.
            /// </summary>
            public List<double> Probabilities { get; set; }
        }
    }
}
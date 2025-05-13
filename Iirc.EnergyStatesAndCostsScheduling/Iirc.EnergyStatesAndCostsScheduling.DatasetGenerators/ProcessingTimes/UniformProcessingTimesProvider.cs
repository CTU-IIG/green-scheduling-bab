// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.ProcessingTimes
{
    using System;
    using System.Collections.Generic;
    using Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.Interface;

    /// <summary>
    /// Provider for the processing times that samples the processing times from a discrete uniform distribution.
    /// [minProcessingTime, maxProcessingTime].
    /// </summary>
    public class UniformProcessingTimesProvider : IProcessingTimesProvider
    {
        public UniformProcessingTimesProvider(int minProcessingTime, int maxProcessingTime, Random rnd)
        {
            this.MinProcessingTime = minProcessingTime;
            this.MaxProcessingTime = maxProcessingTime;
            this.Rnd = rnd;
        }
        
        public static IProcessingTimesProvider FromConfig(Config config, Random rnd)
        {
            return new UniformProcessingTimesProvider(config.MinProcessingTime, config.MaxProcessingTime, rnd);
        }
        
        protected int MinProcessingTime { get; set; }
        protected int MaxProcessingTime { get; set; }
        protected Random Rnd { get; set; }

        public int Generate()
        {
            return this.Rnd.Next(this.MinProcessingTime, this.MaxProcessingTime + 1);
        }
        
        public Dictionary<string, object> GetConfig()
        {
            return new Dictionary<string, object>
            {
                {"MinProcessingTime", this.MinProcessingTime},
                {"MaxProcessingTime", this.MaxProcessingTime},
            };
        }
        
        public class Config
        {
            /// <summary>
            /// Gets or sets the minimum processing time (inclusive).
            /// </summary>
            public int MinProcessingTime { get; set; }
            /// <summary>
            /// Gets or sets the maximum processing time (inclusive).
            /// </summary>
            public int MaxProcessingTime { get; set; }
        }
    }
}

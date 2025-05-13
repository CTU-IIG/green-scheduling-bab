// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.EnergyCosts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.Interface;
    using Newtonsoft.Json;

    /// <summary>
    /// Provider for the energy costs that takes the costs from JSON file that contains serialized
    /// <see cref="EnergyCosts"/> instance.
    /// </summary>
    public class FileEnergyCostsProvider : IEnergyCostsProvider
    {
        public FileEnergyCostsProvider(List<int> costs, List<DateTime> dates, DateTime fixedFromDate, string filepath)
        {
            this.Costs = costs;
            this.Dates = dates;
            this.FixedFromDate = fixedFromDate;
            this.FileName = Path.GetFileName(filepath);
        }

        public static IEnergyCostsProvider FromConfig(Config config, string energyCostsDir)
        {
            var filePath = Path.Combine(energyCostsDir, config.FileName);
            var energyCosts = JsonConvert.DeserializeObject<EnergyCosts>(File.ReadAllText(filePath));
            return new FileEnergyCostsProvider(energyCosts.Costs, energyCosts.Dates, config.FromDate, filePath);
        }
        
        public List<int> Generate(int intervalsCount, int costRepeatCount)
        {
            return this.Generate(this.FixedFromDate, intervalsCount, costRepeatCount);
        }

        public Dictionary<string, object> GetConfig()
        {
            return new Dictionary<string, object>
            {
                {"FromDate", this.FixedFromDate},
                {"FileName", this.FileName},
            };
        }

        public List<int> Generate(DateTime fromDate, int intervalsCount, int costRepeatCount)
        {
            var fromDateIdx = this.Dates.FindIndex(d => d == fromDate);
            if (fromDateIdx < 0)
            {
                throw new ArgumentException($"Date {fromDate} not found.");
            }
            
            var costs = new List<int>();

            int currIdx = fromDateIdx;
            while (true)
            {
                for (int repeat = 0; repeat < costRepeatCount; repeat++)
                {
                    if (costs.Count == intervalsCount)
                    {
                        return costs;
                    }

                    if (currIdx >= this.Costs.Count)
                    {
                        currIdx = fromDateIdx;
                    }
                    
                    costs.Add(this.Costs[currIdx]);
                }

                currIdx++;
            }
        }
        
        protected List<int> Costs { get; set; }
        protected List<DateTime> Dates { get; set; }
        protected DateTime FixedFromDate { get; set; }
        protected string FileName { get; set; }
        
        /// <summary>
        /// The energy costs.
        /// </summary>
        public class EnergyCosts
        {
            /// <summary>
            /// The list of dates and times.
            /// </summary>
            public List<DateTime> Dates { get; set; }
            
            /// <summary>
            /// The list of energy costs corresponding to <see cref="Dates"/>.
            /// </summary>
            public List<int> Costs { get; set; }
        }
        
        public class Config
        {
            /// <summary>
            /// The energy costs file to read. The file is expected to be in PRESCRIPTION_DIR/energy-costs.
            /// </summary>
            public string FileName { get; set; }
            
            /// <summary>
            /// From which date in the energy cost file the cost should be generated.
            /// </summary>
            public DateTime FromDate { get; set; }
        }
        
    }
}
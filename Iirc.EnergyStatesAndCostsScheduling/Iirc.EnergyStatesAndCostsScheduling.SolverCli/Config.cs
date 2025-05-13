// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.SolverCli
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.DataStructs;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Output;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The program configuration.
    /// </summary>
    /// <remarks>If the properties are not provided, the default values from <see cref="SolverConfig"/> are used.
    /// </remarks>
    public class Config
    {
        /// <summary>
        /// Gets or sets the random seed for the solvers.
        /// </summary>
        public int? RandomSeed { get; set; }     
        
        /// <summary>
        /// Gets or sets the time-limit given to the solver for each instance. If the time-limit is not defined, then
        /// the solver may run indefinitely.
        /// </summary>
        public TimeSpan? TimeLimit { get; set; }     

        /// <summary>
        /// Gets or sets the name of the solver class, see
        /// <see cref="Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers"/> namespace for the available
        /// solvers.
        /// </summary>
        public string SolverName { get; set; }     
        
        /// <summary>
        /// Gets or sets the initial start times of the jobs.
        /// If both <see cref="InitStartTimes"/> and <see cref="InitStartTimesFromResult"/> are provided, then
        /// <see cref="InitStartTimes"/> are used.
        /// </summary>
        public List<StartTimes.IndexedStartTime> InitStartTimes { get; set; }     
        
        /// <summary>
        /// Gets or sets a path to result file from which to take the initial start times.
        /// If both <see cref="InitStartTimes"/> and <see cref="InitStartTimesFromResult"/> are provided, then
        /// <see cref="InitStartTimes"/> are used.
        /// </summary>
        public string InitStartTimesFromResult { get; set; }
        
        /// <summary>
        /// Gets or sets a number of parallel workers used by the solver.
        /// </summary>
        public int? NumWorkers { get; set; }
        
        /// <summary>
        /// Gets or sets the presolve level. If not specified, the default solver settings are used.
        /// </summary>
        public PresolveLevel? PresolveLevel { get; set; }
        
        /// <summary>
        /// Gets or sets the solver configuration that is specific for the solver (see the specialized configuration
        /// class contained in the solvers for more details).
        /// </summary>
        public JObject SpecializedSolverConfig { get; set; }     
        
        /// <summary>
        /// Gets or sets a value indicating whether to use the serialized extended instance. If false, the extended
        /// instance will be generated if the solver needs it.
        /// </summary>
        [DefaultValue(false)]
        public bool UseSerializedExtendedInstance { get; set; }     

        public SolverConfig ToSolverConfig()
        {
            var solverConfig = new SolverConfig();
            
            if (this.RandomSeed.HasValue)
            {
                solverConfig.Random = new Random(this.RandomSeed.Value);
            }

            if (this.TimeLimit.HasValue)
            {
                solverConfig.TimeLimit = this.TimeLimit.Value;
            }

            if (this.NumWorkers.HasValue)
            {
                solverConfig.NumWorkers = this.NumWorkers.Value;
            }
            
            if (this.PresolveLevel.HasValue)
            {
                solverConfig.PresolveLevel = this.PresolveLevel.Value;
            }
            
            if (this.InitStartTimes != null)
            {
              solverConfig.InitStartTimes = this.InitStartTimes;
            }

            if (solverConfig.InitStartTimes == null && this.InitStartTimesFromResult != null)
            {
                var initStartTimesResult = JsonConvert.DeserializeObject<Result>(
                    File.ReadAllText(this.InitStartTimesFromResult));
                solverConfig.InitStartTimes = initStartTimesResult.StartTimes;
            }

            if (this.SpecializedSolverConfig != null)
            {
                solverConfig.SpecializedSolverConfig = this.SpecializedSolverConfig.ToObject<Dictionary<string, object>>();
            }

            return solverConfig;
        }
    }
}
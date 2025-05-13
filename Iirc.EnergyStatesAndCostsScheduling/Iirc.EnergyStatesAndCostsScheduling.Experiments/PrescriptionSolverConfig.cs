// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Experiments
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The solver configuration in the experimental setup.
    /// </summary>
    /// <remarks>
    /// If the properties are not provided, the default values from <see cref="SolverConfig"/> are used.
    /// </remarks>
    public class PrescriptionSolverConfig
    {
        /// <summary>
        /// Gets or sets the random seed.
        /// </summary>
        public int? RandomSeed { get; set; }     
        
        /// <summary>
        /// Gets or sets the time-limit given to the solver for each instance. If the time-limit is not defined, then
        /// the solver may run indefinitely.
        /// </summary>
        public TimeSpan? TimeLimit { get; set; }     
        
        /// <summary>
        /// Gets or sets a number of parallel workers used by the solver.
        /// </summary>
        public int? NumWorkers { get; set; }
        
        /// <summary>
        /// Gets or sets the presolve level.
        /// </summary>
        public PresolveLevel? PresolveLevel { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether to use the serialized extended instance. If false, the extended
        /// instance will be generated if the solver needs it.
        /// </summary>
        [DefaultValue(false)]
        public bool UseSerializedExtendedInstance { get; set; }     
        
        public SolverConfig ToSolverConfig(JObject specializedSolverConfig)
        {
            var solverConfig = new SolverConfig();
            
            if (this.RandomSeed.HasValue)
            {
                solverConfig.Random = new Random(this.RandomSeed.Value);
            }

            if (this.TimeLimit.HasValue)
            {
                solverConfig.TimeLimit = this.TimeLimit;
            }

            if (this.NumWorkers.HasValue)
            {
                solverConfig.NumWorkers = this.NumWorkers.Value;
            }
            
            if (this.PresolveLevel.HasValue)
            {
                solverConfig.PresolveLevel = this.PresolveLevel.Value;
            }
            
            if (specializedSolverConfig != null) {
                solverConfig.SpecializedSolverConfig = specializedSolverConfig.ToObject<Dictionary<string, object>>();
            }

            return solverConfig;
        }

        /// <summary>
        /// Merges two prescriptions. The general prescription behaves as default values that can be overriden by the
        /// specific prescription.
        /// </summary>
        /// <param name="general">The general prescription.</param>
        /// <param name="specific">The specific prescription.</param>
        /// <returns>The merged prescription.</returns>
        public static PrescriptionSolverConfig Merge(PrescriptionSolverConfig general, PrescriptionSolverConfig specific)
        {
            return new PrescriptionSolverConfig
            {
                RandomSeed = specific == null || specific.RandomSeed.HasValue == false ?
                    general.RandomSeed : specific.RandomSeed.Value,
                TimeLimit = specific == null || specific.TimeLimit.HasValue == false ?
                    general.TimeLimit : specific.TimeLimit.Value,
                NumWorkers = specific == null || specific.NumWorkers.HasValue == false ?
                    general.NumWorkers : specific.NumWorkers.Value,
                PresolveLevel = specific == null || specific.PresolveLevel.HasValue == false ?
                    general.PresolveLevel : specific.PresolveLevel.Value,
                UseSerializedExtendedInstance = specific?.UseSerializedExtendedInstance ?? general.UseSerializedExtendedInstance
            };
        }
    }
}

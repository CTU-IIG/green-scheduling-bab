// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Experiments
{
    using System.ComponentModel;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The solver prescription.
    /// </summary>
    public class SolverPrescription
    {
        /// <summary>
        /// Gets or sets the id of the solver. Used for identification of the solver for results.
        /// </summary>
        /// <remarks>Can be any string that can be converted to a file system name. However, two different solvers
        /// within the same experimental prescription cannot have the same id.</remarks>
        [JsonProperty(Required=Required.Always)]
        public string Id { get; set; }     

        /// <summary>
        /// Gets or sets the name of the solver class, see
        /// <see cref="Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers"/> namespace for the available
        /// solvers.
        /// </summary>
        [JsonProperty(Required=Required.Always)]
        public string SolverName { get; set; }     

        /// <summary>
        /// Gets or sets the id of the solver, whose start times will be used as initial start times. If not specified,
        /// the solver starts without the initial start times.
        /// </summary>
        public string InitStartTimesFrom { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether to decrease the time-limit given to solver by the running-time of
        /// the solver providing the initial start times.
        /// </summary>
        public bool DecreaseTimeLimitForInitStartTimes { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether to subtract the extended instance generation from the time-limit.
        /// </summary>
        [DefaultValue(false)]
        public bool SubstractExtendedInstanceGenerationFromTimeLimit { get; set; }
        
        /// <summary>
        /// Gets or sets the solver configuration.
        /// </summary>
        /// <remarks>If some properties of the solver configuration are not provided, the global ones are used, see
        /// <see cref="Prescription.GlobalConfig"/>.
        /// </remarks>
        public PrescriptionSolverConfig Config { get; set; }

        /// <summary>
        /// Gets or sets the specialized solver configuration (see the specialized configuration class contained in the
        /// solvers for more details).
        /// </summary>
        public JObject SpecializedSolverConfig { get; set; }     
    }
}

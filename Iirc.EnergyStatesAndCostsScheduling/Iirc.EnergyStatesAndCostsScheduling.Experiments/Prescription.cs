// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Experiments
{
    using Newtonsoft.Json;

    /// <summary>
    /// Prescription of the experimental setup.
    /// </summary>
    public class Prescription
    {
        /// <summary>
        /// Gets or sets the global config.
        /// </summary>
        public PrescriptionSolverConfig GlobalConfig { get; set; }

        /// <summary>
        /// Gets or sets the dataset names to evaluate.
        /// </summary>
        [JsonProperty(Required = Required.Always)]
        public string[] DatasetNames { get; set; }

        /// <summary>
        /// Gets or sets the prescription of the solvers.
        /// </summary>
        /// <remarks>Currently, there is no dependency handling (required by init start times) between the solvers, the
        /// array must be a topological ordering of the solvers (e.g., solver on the index zero has no dependencies).
        /// </remarks>
        [JsonProperty(Required = Required.Always)]
        public SolverPrescription[] Solvers { get; set; }
    }
}

// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators
{
    using System.ComponentModel;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Prescription on how to generate the dataset.
    /// </summary>
    public class Prescription
    {
        /// <summary>
        /// Gets or sets the name of the dataset generator class, see
        /// <see cref="Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.Generators"/> namespace for the available
        /// generators.
        /// </summary>
        public string DatasetGeneratorName { get; set; }

        /// <summary>
        /// Gets or sets the number of instances to randomly generate from fixed parameters.
        /// </summary>
        public int RepetitionsCount { get; set; }

        /// <summary>
        /// Gets or sets the seed used for the random generator. If not specified, a random value is used.
        /// </summary>
        [DefaultValue(null)]
        public int? RandomSeed { get; set; }

        /// <summary>
        /// Gets or sets the prescription that is specific for the dataset generator (see the specialized prescription
        /// class contained in the generators for more details).
        /// </summary>
        public JObject SpecializedPrescription { get; set; }
    }
}
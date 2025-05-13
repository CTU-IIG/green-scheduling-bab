// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.Interface
{
    using System.Collections.Generic;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;

    public interface IDatasetGenerator
    {
        IEnumerable<Instance> GenerateInstances(Prescription prescription, string prescriptionPath);
    }
}
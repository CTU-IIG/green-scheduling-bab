// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers
{
    using System.ComponentModel;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;

    public class CpIsomorphism : PythonScript<CpIsomorphism.SpecializedSolverConfig>
    {
        public CpIsomorphism() : base("cp_isomorphism")
        {
        }

        protected override void CheckInstanceValidity()
        {
            base.CheckSingleMachineInstance();
        }

        protected override void SetInstance(Instance instance)
        {
            base.SetInstance(instance);

            this.Instance.ComputeOptimalSwitchingCosts();
            this.Instance.ComputeGapsLowerBounds();
        }

        public class SpecializedSolverConfig
        {
            [DefaultValue(null)]
            public double? FailureDirectedSearchEmphasis { get; set; }
        }
    }
}
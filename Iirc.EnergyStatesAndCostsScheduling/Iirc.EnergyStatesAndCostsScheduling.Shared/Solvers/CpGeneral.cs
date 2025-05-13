// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers
{
    using System.ComponentModel;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;

    public class CpGeneral : PythonScript<CpGeneral.SpecializedSolverConfig>
    {
        public CpGeneral() : base("cp_general")
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
            [DefaultValue(JobInObjectiveModelling.Optional)]
            public JobInObjectiveModelling JobInObjectiveModelling { get; set; }
            
            [DefaultValue(GapsInObjectiveModelling.Free)]
            public GapsInObjectiveModelling GapsInObjectiveModelling { get; set; }
            
            [DefaultValue(FillAllModelling.StartOfNext)]
            public FillAllModelling FillAllModelling { get; set; }
            
            [DefaultValue(null)]
            public double? FailureDirectedSearchEmphasis { get; set; }
        }

        public enum JobInObjectiveModelling
        {
            Optional = 0,
            Logical = 1,
            Element = 2,
            Overlap = 3,
            StepFunction = 4
        }
        
        public enum GapsInObjectiveModelling
        {
            Fixed = 0,
            Free = 1,
            No = 2
        }
        
        public enum FillAllModelling
        {
            SumLengths = 0,
            Pulse = 1,
            StartOfNext = 2,
        }
    }
}
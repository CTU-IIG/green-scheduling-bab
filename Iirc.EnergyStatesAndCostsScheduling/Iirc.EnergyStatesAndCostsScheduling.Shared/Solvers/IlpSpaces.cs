// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers
{
    using System.ComponentModel;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;

    /// <summary>
    /// The ILP model (ILP-SPACES) introduced in [Benedikt2020a].
    /// </summary>
    public class IlpSpaces : PythonScript<IlpSpaces.SpecializedSolverConfig>
    {
        public IlpSpaces() : base("ilp_spaces")
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
            [DefaultValue(false)] public bool ForbidConsecutiveGaps { get; set; }
            [DefaultValue(true)] public bool PruneByUpperBound { get; set; }
            [DefaultValue(false)] public bool ForceJobsOrdering { get; set; }
            [DefaultValue(false)] public bool ForceJobsOrdering2 { get; set; }
            [DefaultValue(true)] public bool RelaxedJobsOrdering { get; set; }
            [DefaultValue(false)] public bool PruneByLinearRelaxation { get; set; }
            [DefaultValue(false)] public bool SingleConstrForBorderGaps { get; set; }
            [DefaultValue(false)] public bool SparsifyMatrix { get; set; }
            [DefaultValue(false)] public bool SearchJobsFirst { get; set; }
        }
    }
}
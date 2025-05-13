// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;

    /// <summary>
    /// The constructive heuristic algorithm introduced in [Benedikt2020b].
    /// </summary>
    public class ConstructiveHeuristic : BaseCppSolver<ConstructiveHeuristic.SpecializedSolverConfig>
    {
        public ConstructiveHeuristic() : base("ConstructiveHeuristic")
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
        }
        
        protected override void WriteSpecializedSolverConfig(string filePath)
        {
            using (var stream = new StreamWriter(filePath))
            {
                stream.WriteLine((int)this.specializedSolverConfig.Algorithm);
                stream.WriteLine((int)this.specializedSolverConfig.JobsOrdering);
                stream.WriteLine(10); // TODO: probably will be changed to ratio
            }
        }

        public class SpecializedSolverConfig
        {
            [DefaultValue(ConstructiveHeuristic.Algorithm.AllPositionsWithBlockKeeping)]
            public Algorithm Algorithm { get; set; }
            
            [DefaultValue(ConstructiveHeuristic.JobsOrdering.Random)]
            public JobsOrdering JobsOrdering { get; set; }
        }

        public enum Algorithm
        {
            AllPositions = 0,
            RandomPositions = 1,
            AllPositionsWithBlockKeeping = 2
        }
        
        public enum JobsOrdering
        {
            Random = 0,
            ShortestProcessingTimeFirst = 1,
            LongestProcessingTimeFirst = 2,
            AlternateShortestLongestProcessingTime = 3,
            AlternateHalvesShortLongProcessingTime = 4
        }
    }
}
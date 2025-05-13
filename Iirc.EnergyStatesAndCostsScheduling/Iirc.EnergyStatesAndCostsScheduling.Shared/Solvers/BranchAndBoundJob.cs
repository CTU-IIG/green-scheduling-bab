// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;

    /// <summary>
    /// The branch-and-bound algorithm introduced in [Benedikt2020b].
    /// </summary>
    public class BranchAndBoundJob : BaseCppSolver<BranchAndBoundJob.SpecializedSolverConfig>
    {
        public BranchAndBoundJob() : base("BranchAndBoundJob")
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
                stream.WriteLine(this.specializedSolverConfig.UsePrimalHeuristicBlockDetection ? 1 : 0);
                stream.WriteLine(this.specializedSolverConfig.UsePrimalHeuristicPackToBlocksByCp ? 1 : 0);
                stream.WriteLine(this.specializedSolverConfig.PrimalHeuristicPackToBlocksByCpAllJobs ? 1 : 0);
                stream.WriteLine(this.specializedSolverConfig.UseIterativeDeepening ? 1 : 0);
                stream.WriteLine((int)this.specializedSolverConfig.PrimalHeuristicBlockFinding);
                stream.WriteLine((int)this.specializedSolverConfig.PrimalHeuristicBlockFindingStrategy);
                stream.WriteLine((int)this.specializedSolverConfig.JobsJoiningOnGcd);
                stream.WriteLine((int)this.specializedSolverConfig.BranchPriority);
                if (this.specializedSolverConfig.IterativeDeepeningTimeLimit.HasValue)
                {
                    stream.WriteLine($"{(long)this.specializedSolverConfig.IterativeDeepeningTimeLimit.Value.TotalMilliseconds}");
                }
                else
                {
                    stream.WriteLine("-1");
                }
                if (this.specializedSolverConfig.FullHorizonBabNodesCountLimit.HasValue)
                {
                    stream.WriteLine($"{this.specializedSolverConfig.FullHorizonBabNodesCountLimit.Value}");
                }
                else
                {
                    stream.WriteLine("-1");
                }
            }
        }

        public class SpecializedSolverConfig
        {
            [DefaultValue(true)]
            public bool UsePrimalHeuristicBlockDetection { get; set; }
            
            [DefaultValue(true)]
            public bool UsePrimalHeuristicPackToBlocksByCp { get; set; }
            
            [DefaultValue(true)]
            public bool PrimalHeuristicPackToBlocksByCpAllJobs { get; set; }
            
            [DefaultValue(true)]
            public bool UseIterativeDeepening { get; set; }
            
            [DefaultValue(BranchAndBoundJob.JobsJoiningOnGcd.WholeTree)]
            public JobsJoiningOnGcd JobsJoiningOnGcd { get; set; }
            
            [DefaultValue(BranchAndBoundJob.BranchPriority.DynamicByBlockFitting)]
            public BranchPriority BranchPriority { get; set; }
            
            [DefaultValue(BranchAndBoundJob.PrimalHeuristicBlockFinding.WholeTree)]
            public PrimalHeuristicBlockFinding PrimalHeuristicBlockFinding { get; set; }
            
            [DefaultValue(BranchAndBoundJob.PrimalHeuristicBlockFindingStrategy.MinimizeLengthDifference)]
            public PrimalHeuristicBlockFindingStrategy PrimalHeuristicBlockFindingStrategy { get; set; }
            
            [DefaultValue(null)]
            public TimeSpan? IterativeDeepeningTimeLimit { get; set; }
            
            [DefaultValue(null)]
            public long? FullHorizonBabNodesCountLimit { get; set; }
        }

        public enum JobsJoiningOnGcd
        {
            Off = 0,
            Root = 1,
            WholeTree = 2
        }

        public enum BranchPriority
        {
            Random = 0,
            ForcedSpace = 1,
            JoinToPrev = 2,
            DynamicByBlockFitting = 3,
        }

        public enum PrimalHeuristicBlockFinding
        {
            Off = 0,
            Root = 1,
            WholeTree = 2
        }

        public enum PrimalHeuristicBlockFindingStrategy
        {
            MinimizeLengthDifference = 0
        }
    }
}
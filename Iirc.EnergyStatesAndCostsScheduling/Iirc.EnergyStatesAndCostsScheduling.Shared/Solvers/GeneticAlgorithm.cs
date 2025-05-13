// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers
{
    using System;
    using System.ComponentModel;
    using System.IO;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;

    public class GeneticAlgorithm : BaseCppSolver<GeneticAlgorithm.SpecializedSolverConfig>
    {
        public GeneticAlgorithm() : base("GeneticAlgorithm")
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
                stream.WriteLine(this.specializedSolverConfig.GenerationsCount);
                stream.WriteLine(this.specializedSolverConfig.PopulationSize);
                stream.WriteLine(this.specializedSolverConfig.EliteCount);
                stream.WriteLine(this.specializedSolverConfig.CrossoverFraction);
                stream.WriteLine((int)this.specializedSolverConfig.CrossoverStrategy);
                stream.WriteLine((int)this.specializedSolverConfig.MutationStrategy);
                stream.WriteLine(this.specializedSolverConfig.MutationRate);
                stream.WriteLine((int)this.specializedSolverConfig.BestStallMax);
                stream.WriteLine((int)this.specializedSolverConfig.AverageStallMax);
            }
        }

        public class SpecializedSolverConfig
        {
            [DefaultValue(40)]
            public int GenerationsCount { get; set; }
            
            [DefaultValue(20)]
            public int PopulationSize { get; set; }
            
            [DefaultValue(10)]
            public int EliteCount { get; set; }
            
            [DefaultValue(0.7)]
            public double CrossoverFraction { get; set; }
            
            [DefaultValue(GeneticAlgorithm.CrossoverStrategy.Sequential)]
            public CrossoverStrategy CrossoverStrategy { get; set; }
            
            [DefaultValue(GeneticAlgorithm.MutationStrategy.SelectStrategyRandomly)]
            public MutationStrategy MutationStrategy { get; set; }
            
            [DefaultValue(0.4)]
            public double MutationRate { get; set; }
            
            [DefaultValue(10)]
            public int BestStallMax { get; set; }
            
            [DefaultValue(10)]
            public int AverageStallMax { get; set; }
        }
        
        public enum MutationStrategy
        {
            Swap = 0,
            BlockInsertion = 1,
            SelectStrategyRandomly = 2,
            SwapDiffProcTimes = 2,
        }

        public enum CrossoverStrategy
        {
            Sequential = 0,
            TwoPoint = 1
        }
    }
}
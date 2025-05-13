// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers
{
    using System;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.DataStructs;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;
    using Iirc.Utils.SolverFoundations;

    public class LocalSearch : BaseSolver<LocalSearch.SpecializedSolverConfig>
    {
        private Algorithms.LocalSearch algorithm;
        
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
        
        protected override Status Solve()
        {
            this.algorithm = new Algorithms.LocalSearch(
                this.Instance,
                this.SolverConfig.Random);

            StartTimes initStartTimes = null;
            if (this.SolverConfig.InitStartTimes != null)
            {
                initStartTimes = new StartTimes(this.Instance, this.SolverConfig.InitStartTimes);
            }
            
            this.algorithm.SetInput(
                randomSwapNeighborsCount: this.specializedSolverConfig.randomSwapNeighborsCount,
                randomInsertionNeighborsCount: this.specializedSolverConfig.randomInsertionNeighborsCount,
                iterationsCount: this.specializedSolverConfig.iterationsCount,
                restartsCount: this.specializedSolverConfig.restartsCount,
                numWorkers: this.SolverConfig.NumWorkers,
                initStartTimes: initStartTimes);


            return this.algorithm.Solve(timeLimit: this.SolverConfig.TimeLimit);
        }

        protected override bool SolverReachedTimeLimit()
        {
            return this.algorithm.TimeLimitReached;
        }

        protected override TimeSpan? GetTimeToBest()
        {
            return this.algorithm.TimeToBest;
        }
        
        protected override StartTimes GetStartTimes()
        {
            return this.algorithm.StartTimes;
        }
        
        protected override int? GetObjective()
        {
            return this.algorithm.Objective;
        }

        public class SpecializedSolverConfig
        {
            public int? iterationsCount { get; set; }
            public int randomSwapNeighborsCount { get; set; }
            public int randomInsertionNeighborsCount { get; set; }
            public int? restartsCount { get; set; }
        }
   }
}
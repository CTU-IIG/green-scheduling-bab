// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers
{
    using System;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.DataStructs;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;
    using Newtonsoft.Json.Linq;
    using Iirc.Utils.SolverFoundations;
    using Iirc.Utils.Time;
    using Newtonsoft.Json;

    public abstract class BaseSolver<TSpecializedSolverConfig> : ISolver<Instance, SolverConfig, SolverResult>
    {
        protected ExtendedInstance Instance;

        protected SolverConfig SolverConfig;
        
        protected TSpecializedSolverConfig specializedSolverConfig;

        protected Timer timer;

        public SolverResult Solve(
            SolverConfig solverConfig,
            TSpecializedSolverConfig specializedSolverConfig,
            Instance instance)
        {
            this.SolverConfig = solverConfig;
            this.specializedSolverConfig = specializedSolverConfig;
            
            this.timer = new Timer(this.SolverConfig.TimeLimit);
            this.timer.Restart();
            
            this.SetInstance(instance);
            
            this.CheckConfigValidity();
            this.CheckInstanceValidity();
            
            var status = this.Solve();
            StartTimes startTimes = null;
            if (status.IsFeasibleSolution())
            {
                startTimes = this.GetStartTimes();
#if DEBUG
                this.CheckSolution(startTimes);
#endif
            }
            var timeLimitReached = this.SolverReachedTimeLimit();
            var lowerBound = this.GetLowerBound();
            var objective = status.IsFeasibleSolution() ? this.GetObjective() : null;
            this.Cleanup();
            
            this.timer.Stop();
            return new SolverResult
            {
                Status = status,
                StartTimes = startTimes,
                TimeLimitReached = timeLimitReached,
                RunningTime = TimeSpan.FromMilliseconds(this.timer.ElapsedMilliseconds),
                LowerBound = lowerBound,
                Metadata = this.GetResultMetadata(),
                Objective = objective,
                TimeToBest = status.IsFeasibleSolution() ? this.GetTimeToBest() : null,
                AdditionalInfo = this.GetAdditionalInfo()
            };
        }

        public SolverResult Solve(SolverConfig solverConfig, Instance instance)
        {
            var parsedSpecializedSolverConfig = JObject
                .FromObject(solverConfig.SpecializedSolverConfig)
                .ToObject<TSpecializedSolverConfig>(new JsonSerializer { DefaultValueHandling = DefaultValueHandling.Populate});
            return this.Solve(solverConfig, parsedSpecializedSolverConfig, instance);
        }

        protected TimeSpan? RemainingTime
        {
            get
            {
                return this.timer.RemainingTime;
            }
        }
        
        protected bool TimeLimitReached
        {
            get
            {
                return this.timer.TimeLimitReached;
            }
        }
        
        protected virtual void SetInstance(Instance instance)
        {
            this.Instance = ExtendedInstance.GetExtendedInstance(instance);
        }

        protected virtual void CheckInstanceValidity()
        {
        }

        protected void CheckSingleMachineInstance()
        {
            if (this.Instance.MachinesCount != 1)
            {
                throw new ArgumentException("Solver can handle only single machine instances.");
            }
        }
        
        protected virtual void CheckConfigValidity()
        {
        }
        
        protected virtual void Cleanup()
        {
        }

        protected virtual void CheckSolution(StartTimes startTimes)
        {
        }
        
        protected virtual double? GetLowerBound()
        {
            return null;
        }
        
        protected virtual int? GetObjective()
        {
            return null;
        }
        
        protected virtual TimeSpan? GetTimeToBest()
        {
            return null;
        }
        
        protected virtual object GetResultMetadata()
        {
            return null;
        }
        
        protected virtual object GetAdditionalInfo()
        {
            return null;
        }

        protected abstract Status Solve();
        
        protected abstract StartTimes GetStartTimes();
        
        protected abstract bool SolverReachedTimeLimit();
    }
}
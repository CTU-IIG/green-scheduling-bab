// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers
{
    using System;
    using Iirc.Utils.Math;
    using Iirc.Utils.Gurobi;
    using Iirc.Utils.SolverFoundations;
    using Gurobi;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.DataStructs;

    abstract public class BaseMilpSolver<TSpecializedSolverConfig, TVars> : BaseSolver<TSpecializedSolverConfig>
    {
        protected GRBEnv env;

        protected GRBModel model;

        protected TVars vars;

        protected override Status Solve()
        {
            this.env = new GRBEnv();
            this.model = new GRBModel(this.env);

            this.SetModelParameters();
            this.CreateVariables();
            this.CreateConstraints();
            this.CreateObjective();
            this.SetInitStartTimes();
            this.model.SetTimeLimit(this.RemainingTime);
            this.model.Optimize();

            return this.model.GetResultStatus();
        }

        protected override void Cleanup()
        {
            this.model.Dispose();
            this.env.Dispose();
        }

        protected override bool SolverReachedTimeLimit()
        {
            return this.model.TimeLimitReached();
        }

        protected override void CheckInstanceValidity()
        {
        }

        protected virtual void SetModelParameters()
        {
            this.model.Parameters.FeasibilityTol = NumericComparer.DefaultTolerance;
            this.model.Parameters.Threads = Math.Max(0, this.SolverConfig.NumWorkers);
            this.model.Parameters.MIPGap = 0;

            switch (this.SolverConfig.PresolveLevel)
            {
                case PresolveLevel.Auto:
                    this.model.Parameters.Presolve = -1;
                    break;
                case PresolveLevel.Off:
                    this.model.Parameters.Presolve = 0;
                    break;
                case PresolveLevel.Conservative:
                    this.model.Parameters.Presolve = 1;
                    break;
                case PresolveLevel.Aggressive:
                    this.model.Parameters.Presolve = 2;
                    break;
            }
        }

        protected override double? GetLowerBound()
        {
            return this.model.ObjBound;
        }
        
        private void SetInitStartTimes()
        {
            if (this.SolverConfig.InitStartTimes == null)
            {
                return;
            }

            this.SetInitStartTimes(new StartTimes(this.Instance, this.SolverConfig.InitStartTimes));
        }

        protected virtual void SetInitStartTimes(StartTimes startTimes)
        {
        }

        abstract protected void CreateVariables();

        abstract protected void CreateConstraints();

        abstract protected void CreateObjective();

        abstract protected override StartTimes GetStartTimes();
    }
}

// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Output
{
    using System;
    using System.Collections.Generic;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.DataStructs;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers;
    using Iirc.Utils.SolverFoundations;

    /// <summary>
    /// The result of running a solver on an instance.
    /// </summary>
    public class Result
    {
        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public Status Status { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether time limit was reached, i.e., solver had to be interrupted.
        /// </summary>
        public bool TimeLimitReached { get; set; }

        /// <summary>
        /// Gets or sets the running time of the solver.
        /// </summary>
        public TimeSpan RunningTime { get; set; }

        /// <summary>
        /// Gets or sets the found start times of the jobs.
        /// </summary>
        public List<StartTimes.IndexedStartTime> StartTimes { get; set; }
        
        /// <summary>
        /// Gets or sets the achieved lower bound.
        /// </summary>
        public double? LowerBound { get; set; }
        
        /// <summary>
        /// Gets or sets the objective. May be null if the solver decided not to fill it (even for feasible solutions).
        /// </summary>
        public int? Objective { get; set; }
        
        /// <summary>
        /// Gets or sets the time needed to find the best solution.
        /// </summary>
        public TimeSpan? TimeToBest { get; set; }
        
        /// <summary>
        /// Gets or sets the metadata from the solver.
        /// </summary>
        public object Metadata { get; set; }
        
        /// <summary>
        /// Gets or sets the additional result info.
        /// </summary>
        public object AdditionalInfo { get; set; }

        public static Result FromSolverResult(SolverResult solverResult)
        {
            return new Result
            {
                Status = solverResult.Status,
                TimeLimitReached = solverResult.TimeLimitReached,
                RunningTime = solverResult.RunningTime,
                StartTimes = solverResult.StartTimes?.ToIndexedStartTimes(),
                LowerBound = solverResult.LowerBound,
                Objective = solverResult.Objective,
                TimeToBest = solverResult.TimeToBest,
                Metadata = solverResult.Metadata,
                AdditionalInfo = solverResult.AdditionalInfo
            };
        }
    }
}
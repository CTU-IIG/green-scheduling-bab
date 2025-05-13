// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="ISolverResult.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.Utils.SolverFoundations
{
    public enum Status
    {
        NoSolution = 0,

        Optimal = 1,

        Infeasible = 2,

        Heuristic = 3
    }

    public interface ISolverResult
    {
        Status Status { get; set; }
        bool TimeLimitReached { get; set; }
    }
}
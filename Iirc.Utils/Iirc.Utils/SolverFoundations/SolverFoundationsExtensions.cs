// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="SolverFoundationsExtensions.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.Utils.SolverFoundations
{
    public static class SolverFoundationsExtensions
    {
        public static bool IsFeasibleSolution(this Status status)
        {
            return status == Status.Optimal || status == Status.Heuristic;
        }
    }
}

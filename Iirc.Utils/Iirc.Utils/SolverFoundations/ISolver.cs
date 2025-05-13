// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="ISolver.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.Utils.SolverFoundations
{
    public interface ISolver<Instance, SolverConfig, SolverResult>
        where Instance : IInstance
        where SolverConfig : ISolverConfig
        where SolverResult : ISolverResult
    {
        SolverResult Solve(SolverConfig solverConfig, Instance instance);
    }
}
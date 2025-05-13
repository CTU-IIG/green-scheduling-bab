// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="IAlgorithm.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2019 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.Utils.SolverFoundations
{
    using System;

    public interface IAlgorithm
    {
        Status Solve(TimeSpan? timeLimit = null);
        
        bool TimeLimitReached { get; }
    }
}

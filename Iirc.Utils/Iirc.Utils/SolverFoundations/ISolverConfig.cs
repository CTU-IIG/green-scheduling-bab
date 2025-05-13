// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="ISolverConfig.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.Utils.SolverFoundations
{
    using System;
    using System.Collections.Generic;

    public interface ISolverConfig
    {
        TimeSpan? TimeLimit { get; set; }

        Dictionary<string, object> SpecializedSolverConfig { get; set; }
    }
}
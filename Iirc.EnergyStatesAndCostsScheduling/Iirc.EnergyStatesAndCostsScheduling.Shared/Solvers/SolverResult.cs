// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers
{
    using System;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.DataStructs;
    using Iirc.Utils.SolverFoundations;

    public class SolverResult : ISolverResult
    {
        public Status Status { get; set; }

        public bool TimeLimitReached { get; set; }

        public TimeSpan RunningTime { get; set; }
        
        public StartTimes StartTimes { get; set; }
        
        public double? LowerBound { get; set; }
        
        public int? Objective { get; set; }
        
        public TimeSpan? TimeToBest { get; set; }
        
        public object AdditionalInfo { get; set; }
        
        public object Metadata { get; set; }
    }
}
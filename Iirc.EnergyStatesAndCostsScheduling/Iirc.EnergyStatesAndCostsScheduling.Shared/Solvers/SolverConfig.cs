// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers
{
    using System;
    using System.Collections.Generic;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.DataStructs;
    using Iirc.Utils.SolverFoundations;

    /// <summary>
    /// The configuration of the solver.
    /// </summary>
    [Serializable]
    public class SolverConfig : ISolverConfig
    {
        public SolverConfig()
        {
            this.TimeLimit = null;
            this.SpecializedSolverConfig = new Dictionary<string, object>();
            this.NumWorkers = 0;
            this.StopOnFeasibleSolution = false;
            this.Random = new Random();
            this.PresolveLevel = PresolveLevel.Auto;
        }
        
        /// <summary>
        /// Gets or sets the random generator.
        /// </summary>
        public Random Random { get; set; }

        /// <summary>
        /// Gets or sets the time-limit given to the solver. If not specified, the solver can run indefinitely.
        /// </summary>
        public TimeSpan? TimeLimit { get; set; }

        /// <summary>
        /// Gets or sets the solver configuration that is specific for the solver (see the specialized configuration
        /// class contained in the solvers for more details).
        /// </summary>
        public Dictionary<string, object> SpecializedSolverConfig { get; set; }
        
        /// <summary>
        /// Gets or sets a number of parallel workers used by the solver. Default is 0, meaning that the default
        /// settings of the solver is used.
        /// </summary>
        public int NumWorkers { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether a solver should stop once a feasible solution is found.
        /// </summary>
        public bool StopOnFeasibleSolution { get; set; }
        
        /// <summary>
        /// Gets or sets the initial start times of the jobs. Default is null, i.e., no start times.
        /// </summary>
        public List<StartTimes.IndexedStartTime> InitStartTimes { get; set; }
        
        /// <summary>
        /// Gets or sets the value indicating the presolve level.
        /// </summary>
        public PresolveLevel PresolveLevel { get; set; }

        public SolverConfig ShallowCopy()
        {
            return (SolverConfig)this.MemberwiseClone();
        }
    }

    public enum PresolveLevel
    {
        Auto = -1,
        Off = 0,
        Conservative = 1,
        Aggressive = 2
    }
}
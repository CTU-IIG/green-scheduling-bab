// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers
{
    using System;
    using Newtonsoft.Json;
    using System.Diagnostics;
    using System.IO;
    using Iirc.Utils.SolverFoundations;
    using System.Collections.Generic;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.DataStructs;

    public abstract class PythonScript<TSpecializedSolverConfig> : BaseSolver<TSpecializedSolverConfig>
    {
        private readonly string solverName;

        private string solverConfigPath;
        private string specializedSolverConfigPath;
        private string instancePath;
        private string solverResultPath;
        private SolverScriptResult solverScriptResult;

        /// <summary>
        /// </summary>
        /// <param name="solverName">The name of the Python solver (corresponds to the solver filename without the
        /// extension).</param>
        protected PythonScript(string solverName)
        {
            this.solverName = solverName;
        }

        protected string PythonBinPath
        {
            get
            {
                return Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "python");
            }
        }

        protected string SolversModulePath
        {
            get
            {
                return "solvers";
            }
        }

        protected string SolverModulePath
        {
            get
            {
                return $"{this.SolversModulePath}.{this.solverName}";
            }
        }

        protected override Status Solve()
        {
            this.solverConfigPath = Path.GetTempFileName();
            this.specializedSolverConfigPath = Path.GetTempFileName();
            this.instancePath = Path.GetTempFileName();
            this.solverResultPath = Path.GetTempFileName();
            
            File.WriteAllText(this.solverConfigPath, JsonConvert.SerializeObject(this.SolverConfig));
            File.WriteAllText(this.specializedSolverConfigPath, JsonConvert.SerializeObject(this.specializedSolverConfig));
            File.WriteAllText(this.instancePath, JsonConvert.SerializeObject(this.Instance));

            var process = new Process();
            process.StartInfo.FileName = "python3";
            process.StartInfo.CreateNoWindow = false;
            process.StartInfo.WorkingDirectory = this.PythonBinPath;
            process.StartInfo.Arguments = $"-m {this.SolverModulePath} {this.solverConfigPath} {this.specializedSolverConfigPath} {this.instancePath} {this.solverResultPath}";
            process.StartInfo.RedirectStandardInput = true;

            process.Start();
            process.WaitForExit();

            this.solverScriptResult =
                JsonConvert.DeserializeObject<SolverScriptResult>(File.ReadAllText(this.solverResultPath));

            return this.solverScriptResult.Status;
        }

        protected override void Cleanup()
        {
            File.Delete(this.solverConfigPath);
            File.Delete(this.specializedSolverConfigPath);
            File.Delete(this.instancePath);
            File.Delete(this.solverResultPath);
        }

        protected override double? GetLowerBound()
        {
            return this.solverScriptResult.LowerBound;
        }
        
        protected override int? GetObjective()
        {
            return this.solverScriptResult.Objective;
        }
        
        protected override StartTimes GetStartTimes()
        {
            return new StartTimes(this.Instance, this.solverScriptResult.StartTimes);
        }

        protected override bool SolverReachedTimeLimit()
        {
            return this.solverScriptResult.TimeLimitReached;
        }
        
        protected override TimeSpan? GetTimeToBest()
        {
            return this.solverScriptResult.TimeToBest;
        }
        
        protected override object GetAdditionalInfo()
        {
            return this.solverScriptResult.AdditionalInfo;
        }

        private class SolverScriptResult
        {
            public Status Status { get; set; }

            public bool TimeLimitReached { get; set; }

            public List<StartTimes.IndexedStartTime> StartTimes { get; set; }
            
            public double? LowerBound { get; set; }
            
            public int? Objective { get; set; }
            
            public TimeSpan? TimeToBest { get; set; }
            
            public object AdditionalInfo { get; set; }
        }
    }
}

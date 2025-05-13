// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using Iirc.Utils.SolverFoundations;
    using System.Collections.Generic;
    using System.Linq;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.DataStructs;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input.Writers;

    public abstract class BaseCppSolver<TSpecializedSolverConfig> : BaseSolver<TSpecializedSolverConfig>
    {
        private readonly string solverName;

        private string solverConfigPath;
        private string specializedSolverConfigPath;
        private string instancePath;
        private string solverResultPath;
        private CppSolverResult cppSolverResult;

        /// <summary>
        /// </summary>
        /// <param name="solverName">The binary filename of the Cpp solver.</param>
        protected BaseCppSolver(string solverName)
        {
            this.solverName = solverName;
        }

        protected string CppSolversBinPath
        {
            get
            {
                return Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "cpp", "bin");
            }
        }

        protected override Status Solve()
        {
            this.solverConfigPath = Path.GetTempFileName();
            this.specializedSolverConfigPath = Path.GetTempFileName();
            this.instancePath = Path.GetTempFileName();
            this.solverResultPath = Path.GetTempFileName();
            
            this.WriteSolverConfig(this.solverConfigPath);
            this.WriteSpecializedSolverConfig(this.specializedSolverConfigPath);
            new TextInputWriter().WriteToPath(this.Instance, this.instancePath);

            var fileName = Path.Combine(this.CppSolversBinPath, this.solverName);
            if (!File.Exists(fileName))
            {
                throw new ArgumentException($"Solver {this.solverName} does not exist, did you compile it? Check cpp/bin directory.");
            }

            var process = new Process
            {
                StartInfo =
                {
                    FileName = fileName,
                    CreateNoWindow = false,
                    Arguments =
                        $"{this.solverConfigPath} {this.specializedSolverConfigPath} {this.instancePath} {this.solverResultPath}",
                    RedirectStandardInput = true
                }
            };

            process.Start();
            process.WaitForExit();

            this.cppSolverResult = this.ReadSolverResult(this.solverResultPath);

            return this.cppSolverResult.Status;
        }

        protected override void Cleanup()
        {
            File.Delete(this.solverConfigPath);
            File.Delete(this.specializedSolverConfigPath);
            File.Delete(this.instancePath);
            File.Delete(this.solverResultPath);
        }

        protected override int? GetObjective()
        {
            return this.cppSolverResult.Objective;
        }
        
        protected override StartTimes GetStartTimes()
        {
            return new StartTimes(this.Instance, this.cppSolverResult.StartTimes);
        }

        protected override bool SolverReachedTimeLimit()
        {
            return this.cppSolverResult.TimeLimitReached;
        }
        
        protected override object GetAdditionalInfo()
        {
            return this.cppSolverResult.AdditionalInfo;
        }
        
        protected override double? GetLowerBound()
        {
            return this.cppSolverResult.Status == Status.Optimal ? (double?) this.cppSolverResult.Objective : null;
        }
        
        protected virtual void WriteSolverConfig(string filePath)
        {
            using (var stream = new StreamWriter(filePath))
            {
                stream.WriteLine($"{(long)this.SolverConfig.Random.Next()}");
                
                if (this.SolverConfig.TimeLimit.HasValue)
                {
                    stream.WriteLine($"{(long)this.RemainingTime.Value.TotalMilliseconds}");
                }
                else
                {
                    stream.WriteLine("-1");
                }
                
                stream.WriteLine($"{this.SolverConfig.NumWorkers}");
                
                if (this.SolverConfig.InitStartTimes != null)
                {
                    stream.WriteLine(this.SolverConfig.InitStartTimes.Count);
                    foreach (var initStartTime in this.SolverConfig.InitStartTimes)
                    {
                        stream.WriteLine($"{initStartTime.JobIndex} {initStartTime.StartTime}");
                    }
                }
                else
                {
                    stream.WriteLine("0");
                }
            }
        }

        protected abstract void WriteSpecializedSolverConfig(string filePath);
        
        protected virtual CppSolverResult ReadSolverResult(string filePath)
        {
            var lines = File.ReadAllLines(filePath).ToList();
            int currLine = 0;

            Status status = Enum.Parse<Status>(lines[currLine].Trim(), true);
            currLine++;

            int? objective = int.Parse(lines[currLine]) < 0 ? (int?)null : int.Parse(lines[currLine]);
            currLine++;

            bool timeLimitReached = int.Parse(lines[currLine]) != 0;
            currLine++;
            
            List<StartTimes.IndexedStartTime> startTimes = null;
            if (lines[currLine].Trim().ToLower() != "nosolution")
            {
                startTimes = new List<StartTimes.IndexedStartTime>();
                var values = lines[currLine]
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Select(v => int.Parse(v))
                    .ToList();

                int ptr = 0;
                while (ptr < values.Count)
                {
                    startTimes.Add(new StartTimes.IndexedStartTime
                    {
                        JobIndex = values[ptr],
                        StartTime = values[ptr + 1],
                    });

                    ptr += 2;
                }

            }
            currLine++;
            
            var additionalInfo = new Dictionary<string, object>();
            
            additionalInfo["NumNodes"] = long.Parse(lines[currLine]);
            currLine++;
            additionalInfo["PrimalHeuristicBlockDetectionFoundSolution"] = long.Parse(lines[currLine]);
            currLine++;
            additionalInfo["PrimalHeuristicPackToBlocksByCpFoundSolution"] = long.Parse(lines[currLine]);
            currLine++;
            additionalInfo["JobsJoinedOnLargerGcd"] = long.Parse(lines[currLine]);
            currLine++;
            additionalInfo["RootLowerBound"] = int.Parse(lines[currLine]);
            currLine++;
            additionalInfo["LowerBoundTotalTime"] = TimeSpan.FromMilliseconds(long.Parse(lines[currLine]));
            currLine++;
            additionalInfo["PrimalHeuristicBlockDetectionTotalTime"] = TimeSpan.FromMilliseconds(long.Parse(lines[currLine]));
            currLine++;
            additionalInfo["PrimalHeuristicPackToBlocksByCpTotalTime"] = TimeSpan.FromMilliseconds(long.Parse(lines[currLine]));
            currLine++;
            additionalInfo["PrimalHeuristicBlockFindingTotalTime"] = TimeSpan.FromMilliseconds(long.Parse(lines[currLine]));
            currLine++;

            return new CppSolverResult
            {
                Status = status,
                Objective = objective,
                TimeLimitReached = timeLimitReached,
                StartTimes = startTimes,
                AdditionalInfo = additionalInfo
            };
        }

        protected class CppSolverResult
        {
            public Status Status { get; set; }

            public bool TimeLimitReached { get; set; }

            public List<StartTimes.IndexedStartTime> StartTimes { get; set; }
            
            public int? Objective { get; set; }
            
            public object AdditionalInfo { get; set; }
        }
    }
}

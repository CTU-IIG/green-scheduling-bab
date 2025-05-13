// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Experiments
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using CommandLine;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Algorithms;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input.Readers;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Output;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers;
    using Iirc.Utils.SolverFoundations;
    using Newtonsoft.Json;

    class Program
    {
        public static int Main(string[] args)
        {
            return new Parser(parserConfig => parserConfig.HelpWriter = Console.Out)
                .ParseArguments<CmdOptions>(args)
                .MapResult(opts => Run(opts), errs => 1);
        }

        private static int Run(CmdOptions opts)
        {
            var prescriptionPath = ExperimentPrescriptionPath(opts);
            if (!File.Exists(prescriptionPath))
            {
                Console.WriteLine($"Experiment prescription file {prescriptionPath} does not exist.");
                return 1;
            }

            var prescription = JsonConvert.DeserializeObject<Prescription>(
                File.ReadAllText(prescriptionPath),
                new JsonSerializerSettings()
                {
                    DefaultValueHandling = DefaultValueHandling.Populate
                });

            if (!Directory.Exists(Program.DatasetsPath(opts)))
            {
                Console.WriteLine($"Datasets directory {Program.DatasetsPath(opts)} does not exist.");
                return 1;
            }
            
            foreach (var datasetName in prescription.DatasetNames)
            {
                var datasetPath = DatasetPath(opts, datasetName);
                if (!Directory.Exists(datasetPath))
                {
                    Console.WriteLine($"Dataset directory {datasetPath} does not exist.");
                    return 1;
                }
            }

            if (opts.FromScratch)
            {
                foreach (var datasetName in prescription.DatasetNames)
                {
                    foreach (var solverPrescription in prescription.Solvers)
                    {
                        var solverResultsPath = ResultsExperimentDatasetSolverPath(
                            opts,
                            datasetName,
                            solverPrescription.Id);
                        if (Directory.Exists(solverResultsPath))
                        {
                            Directory.Delete(solverResultsPath, true);
                        }
                    }
                }
            }
            
            // TODO:
            if (opts.NumThreads != 1)
            {
                Console.WriteLine($"Parallel instance solving is not currently supported due to:");
                Console.WriteLine($"- optimal switching cost is computed in parallel and there is no option to disable this");
                return 1;
            }

            var objectLock = new object();
            foreach (var datasetName in prescription.DatasetNames)
            {
                var instancePaths = Directory.EnumerateFiles(DatasetPath(opts, datasetName)).ToList();
                foreach (var solverPrescription in prescription.Solvers)
                {
                    var prescriptionSolverConfig = PrescriptionSolverConfig.Merge(
                        prescription.GlobalConfig,
                        solverPrescription.Config);
                    
                    Parallel.ForEach(
                        instancePaths,
                        new ParallelOptions { MaxDegreeOfParallelism = opts.NumThreads },
                        (instancePath) => {
                            try
                            {
                                Console.WriteLine($"Solving {instancePath} using {solverPrescription.Id}");

                                var resultPath = ResultPath(
                                    opts, datasetName, solverPrescription.Id, Path.GetFileName(instancePath));

                                if (opts.FromScratch == false && File.Exists(resultPath))
                                {
                                    Console.WriteLine($"{instancePath} using {solverPrescription.Id} already solved");
                                    return;
                                }

                                var instance = new InputReader().ReadFromPath(instancePath);
                                
                                if (!prescriptionSolverConfig.UseSerializedExtendedInstance)
                                {
                                    instance.SerializedExtendedInstance = null;
                                }

                                SolverConfig solverConfig;
                                lock (objectLock)
                                {
                                    solverConfig =
                                        prescriptionSolverConfig.ToSolverConfig(solverPrescription.SpecializedSolverConfig);
                                }

                                if (solverPrescription.InitStartTimesFrom != null)
                                {
                                    var initStartTimesResultPath = ResultPath(
                                        opts,
                                        datasetName,
                                        solverPrescription.InitStartTimesFrom,
                                        Path.GetFileName(instancePath));

                                    var initStartTimesResult = JsonConvert.DeserializeObject<Result>(
                                        File.ReadAllText(initStartTimesResultPath));

                                    if (initStartTimesResult.Status == Status.Optimal
                                        || initStartTimesResult.Status == Status.Heuristic)
                                    {
                                        solverConfig.InitStartTimes = initStartTimesResult.StartTimes;
                                    }

                                    if (solverPrescription.DecreaseTimeLimitForInitStartTimes
                                        && solverConfig.TimeLimit.HasValue)
                                    {
                                        var remainingTime =
                                            solverConfig.TimeLimit.Value - initStartTimesResult.RunningTime;
                                        if (remainingTime < TimeSpan.Zero)
                                        {
                                            remainingTime = TimeSpan.Zero;
                                        }

                                        solverConfig.TimeLimit = remainingTime;
                                    }
                                }

                                if (solverPrescription.SubstractExtendedInstanceGenerationFromTimeLimit
                                    && solverConfig.TimeLimit.HasValue
                                    && instance.TimeForExtendedInstance.HasValue)
                                {
                                    solverConfig.TimeLimit = new TimeSpan(Math.Max(
                                        0,
                                        solverConfig.TimeLimit.Value.Ticks - instance.TimeForExtendedInstance.Value.Ticks));
                                }

                                var solver = new SolverFactory().Create(solverPrescription.SolverName);

                                var solverResult = solver.Solve(solverConfig, instance);

                                if (solverResult.Status == Status.Optimal || solverResult.Status == Status.Heuristic)
                                {
                                    var feasibilityChecker = new FeasibilityChecker();
                                    var feasibilityStatus = feasibilityChecker.Check(instance, solverResult.StartTimes, solverConfig, solverResult.Objective);
                                    if (feasibilityStatus != FeasibilityChecker.FeasibilityStatus.Feasible)
                                    {
                                        throw new Exception($"Feasibility check failed: {feasibilityStatus}, {instancePath}, {solverPrescription.Id}");
                                    }
                                }

                                lock (objectLock)
                                {
                                    if (!Directory.Exists(Path.GetDirectoryName(resultPath)))
                                    {
                                        Directory.CreateDirectory(Path.GetDirectoryName(resultPath));
                                    }
                                }

                                File.WriteAllText(
                                    resultPath,
                                    JsonConvert.SerializeObject(Result.FromSolverResult(solverResult)));
                            }
                            catch (Exception)
                            {
                                Console.WriteLine($"Error while solving {instancePath} using {solverPrescription.Id}");
                                throw;
                            }
                        });
                }
            }

            return 0;
        }

        public static string DatasetsPath(CmdOptions opts)
        {
            return Path.Combine(opts.DataRootPath, "datasets");
        }
        
        public static string DatasetPath(CmdOptions opts, string datasetName)
        {
            return Path.Combine(Program.DatasetsPath(opts), datasetName);
        }
        
        public static string ExperimentsPrescriptionsPath(CmdOptions opts)
        {
            return Path.Combine(opts.DataRootPath, "experiments-prescriptions");
        }
        
        public static string ExperimentPrescriptionPath(CmdOptions opts)
        {
            return Path.Combine(Program.ExperimentsPrescriptionsPath(opts), opts.PrescriptionFile);
        }
        
        public static string ResultsPath(CmdOptions opts)
        {
            return Path.Combine(opts.DataRootPath, "results");
        }
        
        public static string ResultsExperimentPath(CmdOptions opts)
        {
            return Path.Combine(
                Program.ResultsPath(opts),
                Path.GetFileNameWithoutExtension(opts.PrescriptionFile));
        }
        
        public static string ResultsExperimentDatasetPath(CmdOptions opts, string datasetName)
        {
            return Path.Combine(Program.ResultsExperimentPath(opts), datasetName);
        }
        
        public static string ResultsExperimentDatasetSolverPath(CmdOptions opts, string datasetName, string solverId)
        {
            // DATA_PATH/results/{prescription}/{datasetName}/{solverId}
            return Path.Combine(Program.ResultsExperimentDatasetPath(opts, datasetName), solverId);
        }
        
        public static string ResultPath(CmdOptions opts, string datasetName, string solverId, string resultFileName)
        {
            return Path.Combine(ResultsExperimentDatasetSolverPath(opts, datasetName, solverId), resultFileName);
        }
    }
}

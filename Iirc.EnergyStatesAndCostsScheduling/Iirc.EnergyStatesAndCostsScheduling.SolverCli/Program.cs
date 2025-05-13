// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.SolverCli
{
    using CommandLine;
    using System;
    using System.IO;
    using Newtonsoft.Json;
    using Iirc.Utils.SolverFoundations;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Algorithms;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input.Readers;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Output;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers;

    class Program
    {
        public static int Main(string[] args)
        {
            return new Parser(parserConfig => parserConfig.HelpWriter = Console.Out)
                .ParseArguments<CmdOptions>(args)
                .MapResult(
                    opts => Run(opts),
                    errs => 1
                );
        }

        private static int Run(CmdOptions opts)
        {
            try
            {
                var config = GetConfig(opts);
                var solverConfig = config.ToSolverConfig();

                var instance = GetInstance(opts);

                if (config.UseSerializedExtendedInstance)
                {
                    instance = ExtendedInstance.GetExtendedInstance(instance);
                }
                else
                {
                    instance.SerializedExtendedInstance = null;
                }
                
                CheckInstance(instance);

                var solverResult = Solve(config, solverConfig, instance);
                Console.WriteLine($"Running time: {solverResult.RunningTime}");
                if (solverResult.Status == Status.Heuristic || solverResult.Status == Status.Optimal)
                {
                    Console.WriteLine($"Status: {solverResult.Status}");
                    Console.WriteLine($"TEC from solver: {solverResult.Objective}");
                    Console.WriteLine($"Lower bound: {solverResult.LowerBound}");
                    Console.WriteLine($"Additional info: {JsonConvert.SerializeObject(solverResult.AdditionalInfo)}");
                }
                else if (solverResult.Status == Status.Infeasible)
                {
                    Console.WriteLine($"The instance is proven to be infeasible.");
                }
                else if (solverResult.Status == Status.NoSolution)
                {
                    Console.WriteLine($"No solution was found.");
                }
                
                File.WriteAllText(
                    "result.json",
                    JsonConvert.SerializeObject(Result.FromSolverResult(solverResult)));

                return 0;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine(ex.Message);
                return 1;
            }
        }

        private static Config GetConfig(CmdOptions opts)
        {
            if (!File.Exists(opts.ConfigPath))
            {
                throw new FileNotFoundException($"Config file {opts.ConfigPath} does not exist.");
            }

            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(opts.ConfigPath));
        }

        private static Instance GetInstance(CmdOptions opts)
        {
            if (!File.Exists(opts.InstancePath))
            {
                throw new FileNotFoundException($"Instance file {opts.InstancePath} does not exist.");
            }

            return new InputReader().ReadFromPath(opts.InstancePath);
        }

        private static void CheckInstance(Instance instance)
        {
            var instanceChecker = new InstanceChecker();
            var instanceStatus = instanceChecker.Check(instance);

            if (instanceStatus != InstanceChecker.InstanceStatus.Ok)
            {
                throw new Exception($"Incorrect instance: {instanceStatus}");
            }
        }

        private static SolverResult Solve(Config config, SolverConfig solverConfig, Instance instance)
        {
            var solver = new SolverFactory().Create(config.SolverName);
            Console.WriteLine($"Starting solver: {solver.GetType().Name}");
            var solverResult = solver.Solve(solverConfig, instance);
            Console.WriteLine($"Solver finished.");

            if (solverResult.Status == Status.Heuristic || solverResult.Status == Status.Optimal)
            {
                Console.WriteLine($"Solution found, checking its feasibility.");
                var feasibilityChecker = new FeasibilityChecker();
                var feasibilityCheckStatus = feasibilityChecker.Check(
                    instance,
                    solverResult.StartTimes,
                    solverConfig,
                    solverResult.Objective);
                if (feasibilityCheckStatus != FeasibilityChecker.FeasibilityStatus.Feasible)
                {
                    throw new Exception($"Solution not feasible: {feasibilityCheckStatus}");
                }
                Console.WriteLine($"Solution feasible.");
            }
            else
            {
                Console.WriteLine($"No solution found.");
            }
            
            return solverResult;
        }
    }
}
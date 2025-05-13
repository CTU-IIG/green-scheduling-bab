using System;

namespace ExampleGenerator
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.StateDiagrams;
    using Iirc.Utils.Collections;

    class Program
    {
        static void Main(string[] args)
        {
            int instancesCount = 200000;
            
            int[] procTimes = {1, 2, 4};
            var intervalsCount = 20;
            var rnd = new Random();
            for (int instanceIdx = 0; instanceIdx < instancesCount; instanceIdx++)
            {
                var energyCosts = NextEnergyCosts(intervalsCount, rnd);
                var ins = GenerateInstance(procTimes, energyCosts);
                
                var solver = new BranchAndBoundJob();
                
                var solverConfig = new SolverConfig
                {
                    Random = rnd,
                    InitStartTimes =  null,
                    NumWorkers = 0,
                    PresolveLevel = PresolveLevel.Auto,
                    SpecializedSolverConfig = null,
                    StopOnFeasibleSolution = false,
                    TimeLimit = null
                };
                
                var scfg1 = new BranchAndBoundJob.SpecializedSolverConfig
                {
                    JobsJoiningOnGcd = BranchAndBoundJob.JobsJoiningOnGcd.Off,
                    UsePrimalHeuristicBlockDetection = false,
                    UsePrimalHeuristicPackToBlocksByCp = false
                };
                var scfg2 = new BranchAndBoundJob.SpecializedSolverConfig
                {
                    JobsJoiningOnGcd = BranchAndBoundJob.JobsJoiningOnGcd.WholeTree,
                    UsePrimalHeuristicBlockDetection = false,
                    UsePrimalHeuristicPackToBlocksByCp = false
                };
                var scfg3 = new BranchAndBoundJob.SpecializedSolverConfig
                {
                    JobsJoiningOnGcd = BranchAndBoundJob.JobsJoiningOnGcd.WholeTree,
                    UsePrimalHeuristicBlockDetection = true,
                    UsePrimalHeuristicPackToBlocksByCp = false
                };
                var scfg4 = new BranchAndBoundJob.SpecializedSolverConfig
                {
                    JobsJoiningOnGcd = BranchAndBoundJob.JobsJoiningOnGcd.WholeTree,
                    UsePrimalHeuristicBlockDetection = true,
                    UsePrimalHeuristicPackToBlocksByCp = true
                };

                var scfgs = new[] {scfg1, scfg2, scfg3, scfg4};

                int? prevNumNodes = null;
                bool isCandidate = true;
                for (int scfgIdx = 0; scfgIdx < scfgs.Length; scfgIdx++)
                {
                    var scfg = scfgs[scfgIdx];
                    var result = solver.Solve(solverConfig, scfg, ins);
                    var additionalInfo = (Dictionary<string, object>)result.AdditionalInfo;
                    int rootLowerBound = (int) additionalInfo["RootLowerBound"];
                    
                    if (rootLowerBound == result.Objective)
                    {
                        isCandidate = false;
                        break;
                    }
                    
                    int numNodes = (int) additionalInfo["NumNodes"];
                    if (prevNumNodes == null)
                    {
                        prevNumNodes = numNodes;
                    }
                    else
                    {
                        if (prevNumNodes <= numNodes && scfgIdx != 2)
                        {
                            isCandidate = false;
                            break;
                        }
                        
                        prevNumNodes = numNodes;
                    }
                }

                if (isCandidate)
                {
                    File.AppendAllLines("candidates.txt", new [] { string.Join(',', energyCosts.Select(c => c.ToString()))});
                    return;
                }
            }
        }

        private static int[] NextEnergyCosts(int intervalsCount, Random rnd)
        {
            var energyCosts = Enumerable
                .Range(0, intervalsCount)
                .Select(_ => rnd.Next() % 15)
                .ToArray();

            energyCosts[10] = 60;

            return energyCosts;
        }

        private static ExtendedInstance GenerateInstance(int[] procTimes, int[] energyCosts)
        {
            var jobs = procTimes.Select((procTime, idx) => new Job(idx, idx, 0, procTime));
            var intervals = energyCosts.Select((energyCost, idx) => new Interval(idx, idx, idx + 1, energyCost));
            
            var ins = new Instance(
                machinesCount: 1,
                jobs: jobs.ToArray(),
                intervals: intervals.ToArray(),
                lengthInterval: 1,
                stateDiagram: new Aghelinejad2017a());
            
            var eins = new ExtendedInstance(ins);
            eins.GenerateFullExtendedInstance();
            return eins;
        }
    }
}
// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.DataStructs;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Interface;
    using Iirc.Utils.Collections;
    using Iirc.Utils.SolverFoundations;
    using Iirc.Utils.Time;

    public class LocalSearch : IAlgorithm, IHasStartTimes, IHasObjective
    {
        private ExtendedInstance instance;
        private Random rnd;
        private Timer timer;

        private int currentIteration;
        private int currentRestart;

        private int? iterationsCount;
        private int randomSwapNeighborsCount;
        private int randomInsertionNeighborsCount;
        private int numWorkers;

        private int? restartsCount;
        private List<Job> initOrderedJobs;
        private StartTimes initStartTimes;

        private BestSolutionStorage<Individual> bestSolutionStorage;

        public LocalSearch(ExtendedInstance instance, Random rnd)
        {
            this.instance = instance;
            this.rnd = rnd;
        }

        public LocalSearch(Instance instance, Random rnd) : this(
            ExtendedInstance.GetExtendedInstance(instance), rnd)
        {
        }

        public void SetInput(
            int randomSwapNeighborsCount = 20,
            int randomInsertionNeighborsCount = 20,
            int? iterationsCount = null,
            int numWorkers = 0,
            int? restartsCount = 1,
            List<Job> initOrderedJobs = null,
            StartTimes initStartTimes = null)
        {
            this.randomSwapNeighborsCount = randomSwapNeighborsCount;
            this.randomInsertionNeighborsCount = randomInsertionNeighborsCount;
            this.iterationsCount = iterationsCount;
            this.restartsCount = restartsCount;
            this.numWorkers = numWorkers;
            this.initOrderedJobs = initOrderedJobs;
            this.initStartTimes = initStartTimes;
        }
        
        public Status Solve(TimeSpan? timeLimit = null)
        {
            this.timer = new Timer(timeLimit);
            this.timer.Restart();

            this.bestSolutionStorage = new BestSolutionStorage<Individual>();
            this.currentRestart = 0;
            this.currentIteration = 0;

            return this.SolveInternal();
        }

        private Status SolveInternal()
        {
            this.currentRestart = 0;

            while (!this.GlobalStoppingConditionSatisfied())
            {
                this.SearchRestart();
                this.currentRestart++;
            }

            return this.bestSolutionStorage.HasSolution ? Status.Heuristic : Status.NoSolution;
        }

        private void SearchRestart()
        {
            this.currentIteration = 0;

            var incumbent = new Individual();

            // Initialization is used only in the first restart, other restarts use random ordering. Init ordering has
            // larger priority than init start times.
            if ((this.initOrderedJobs != null || this.initStartTimes != null) && this.currentRestart == 0)
            {

                if (this.initStartTimes != null)
                {
                    incumbent.OrderedJobs = this.initStartTimes.GetOrderedJobsOnMachines(this.instance).First();
                }

                if (this.initOrderedJobs != null)
                {
                    incumbent.OrderedJobs = this.initOrderedJobs;
                }
            }
            else
            {
                incumbent.OrderedJobs = this.instance.Jobs.Shuffle(this.rnd).ToList();
            }

            {
                var scheduler = new FixedPermCostComputation(this.instance);
                scheduler.SetInput(incumbent.OrderedJobs);
                var status = scheduler.Solve(this.timer.RemainingTime);
                if (status.IsFeasibleSolution())
                {
                    incumbent.StartTimes = scheduler.StartTimes;
                    incumbent.Objective = scheduler.Objective;
                    this.TestAndUpdateBestSolution(incumbent);
                }
            }

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = this.numWorkers > 0 ? this.numWorkers : Environment.ProcessorCount
            };

            var bestNeighborLock = new object();
            var timerLock = new object();
            while (!this.SearchStoppingConditionSatisfied())
            {
                Individual bestNeighbor = null;

                var perturbations = Enumerable.Empty<object>();
                perturbations = perturbations.Concat(this.GenerateRandomSwaps(incumbent));
                perturbations = perturbations.Concat(this.GenerateRandomInsertions(incumbent));

                var bestNeighborIndex =
                    long.MaxValue; // To make paralelization and update of best solution deterministic.
                Parallel.ForEach(
                    perturbations,
                    parallelOptions,
                    (perturbation, state, neighborIndex) =>
                    {
                        Individual neighbor = null;
                        if (perturbation is Swap)
                        {
                            var swap = (Swap) perturbation;
                            neighbor = swap.Apply(incumbent);
                        }
                        else if (perturbation is Insertion)
                        {
                            var insertion = (Insertion) perturbation;
                            neighbor = insertion.Apply(incumbent);
                        }
                        else
                        {
                            throw new ArgumentException("Unknown perturbation operator");
                        }

                        TimeSpan? remainingTimeForNeighbor = TimeSpan.Zero;
                        lock (timerLock)
                        {
                            remainingTimeForNeighbor = this.timer.RemainingTime;
                        }

                        var status = this.ProcessNeighbor(neighbor, remainingTimeForNeighbor);
                        if (status.IsFeasibleSolution())
                        {
                            lock (bestNeighborLock)
                            {
                                if (bestNeighbor?.StartTimes == null
                                    || neighbor.Objective.Value < bestNeighbor.Objective
                                    || (neighbor.Objective.Value == bestNeighbor.Objective && neighborIndex < bestNeighborIndex))
                                {
                                    bestNeighbor = neighbor;
                                    bestNeighborIndex = neighborIndex;
                                }
                            }
                        }
                    }
                );

                if (bestNeighbor != null)
                {
                    if (incumbent?.StartTimes == null
                        || bestNeighbor.Objective.Value <= incumbent.Objective.Value)
                    {
                        incumbent = bestNeighbor;
                        this.TestAndUpdateBestSolution(incumbent);
                    }
                }

                this.currentIteration++;
            }
        }

        private Status ProcessNeighbor(Individual neighbor, TimeSpan? remainingTime)
        {
            var scheduler = new FixedPermCostComputation(this.instance);
            scheduler.SetInput(neighbor.OrderedJobs);
            var status = scheduler.Solve(remainingTime);

            if (status.IsFeasibleSolution())
            {
                neighbor.StartTimes = scheduler.StartTimes;
                neighbor.Objective = scheduler.Objective;
            }

            return status;
        }

        private void TestAndUpdateBestSolution(Individual incumbent)
        {
            var updated = this.bestSolutionStorage.TestAndUpdateBestSolution(incumbent);
#if DEBUG
            if (updated)
            {
                Console.WriteLine(
                    $"New best solution {this.bestSolutionStorage.BestSolution.Objective} in {this.bestSolutionStorage.TimeToBest.TotalSeconds}s");
            }
#endif
        }

        private IEnumerable<Swap> GenerateRandomSwaps(Individual individual)
        {
            return Enumerable
                .Range(0, this.randomSwapNeighborsCount)
                .Select(_ =>
                {
                    int position1 = this.rnd.Next(0, individual.OrderedJobs.Count);
                    int position2 = this.rnd.Next(0, individual.OrderedJobs.Count);
                    while (individual.OrderedJobs[position1].ProcessingTime ==
                           individual.OrderedJobs[position2].ProcessingTime)
                    {
                        position1 = this.rnd.Next(0, individual.OrderedJobs.Count);
                        position2 = this.rnd.Next(0, individual.OrderedJobs.Count);
                    }
                    return new Swap
                    {
                        Position1 = position1,
                        Position2 = position2
                    };
                });
        }

        private IEnumerable<Insertion> GenerateRandomInsertions(Individual individual)
        {
            return Enumerable
                .Range(0, this.randomInsertionNeighborsCount)
                .Select(_ =>
                {
                    return new Insertion
                    {
                        Src = this.rnd.Next(0, individual.OrderedJobs.Count),
                        Dest = this.rnd.Next(0, individual.OrderedJobs.Count),
                    };
                });
        }

        private bool SearchStoppingConditionSatisfied()
        {
            return this.timer.TimeLimitReached
                   || (this.iterationsCount.HasValue && this.currentIteration >= this.iterationsCount.Value);
        }

        private bool GlobalStoppingConditionSatisfied()
        {
            return this.timer.TimeLimitReached
                   || (this.restartsCount.HasValue && this.currentRestart >= this.restartsCount.Value);
        }

        public StartTimes StartTimes
        {
            get { return this.bestSolutionStorage.BestSolution.StartTimes; }
        }
        
        public int? Objective
        {
            get { return this.bestSolutionStorage.BestSolution.Objective; }
        }

        public TimeSpan? TimeToBest
        {
            get { return this.bestSolutionStorage?.TimeToBest; }
        }

        public List<Job> OrderedJobs
        {
            get { return this.bestSolutionStorage.BestSolution.OrderedJobs; }
        }

        public bool TimeLimitReached
        {
            get { return this.timer.TimeLimitReached; }
        }

        private class Individual : IHasStartTimes, IHasObjective
        {
            private List<Job> orderedJobs;
            
            public List<Job> OrderedJobs
            {
                get
                {
                    return this.orderedJobs;
                }
                
                set
                {
                    this.orderedJobs = value;
                    this.StartTimes = null;
                    this.Objective = null;
                }
            }
            
            public StartTimes StartTimes { get; set; }

            public int? Objective { get; set; }

            public Individual CopyOrdering()
            {
                var copyIndividual = new Individual();
                copyIndividual.OrderedJobs = this.OrderedJobs.ToList();
                return copyIndividual;
            }
        }

        private class Swap
        {
            public int Position1 { get; set; }
            public int Position2 { get; set; }
            public Individual Apply(Individual individual)
            {
                var newIndividual = individual.CopyOrdering();
                newIndividual.OrderedJobs.SwapInPlace(this.Position1, this.Position2);
                return newIndividual;
            }
        }
        
        private class Insertion
        {
            public int Src { get; set; }
            public int Dest { get; set; }
            public Individual Apply(Individual individual)
            {
                var newIndividual = new Individual();
                newIndividual.OrderedJobs = individual.OrderedJobs.MoveElement(
                    this.Src,
                    this.Dest);
                return newIndividual;
            }
        }
    }
}

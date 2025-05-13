// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Algorithms
{
    using System;
    using System.Linq;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.DataStructs;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers;
    using Iirc.Utils.Collections;
    using Iirc.Utils.SolverFoundations;

    public class FeasibilityChecker
    {
        private ExtendedInstance instance;

        private StartTimes startTimes;

        private SolverConfig solverConfig;


        public FeasibilityStatus Status { get; private set; }
        public int? Machine { get; private set; }
        public Job Job { get; private set; }
        public Job NextJob { get; private set; }
        
        public FeasibilityStatus Check(
            Instance instance,
            StartTimes startTimes,
            SolverConfig solverConfig,
            int? objective = null)
        {
            this.instance = ExtendedInstance.GetExtendedInstance(instance);
            this.instance.ComputeOptimalSwitchingCosts();    // For TEC computation.
            
            this.startTimes = startTimes;
            this.solverConfig = solverConfig;

            this.Job = null;
            this.NextJob = null;
            this.Machine = null;
            this.Status = FeasibilityStatus.Unknown;

            var feasible =
                this.EveryJobHasStartTime()
                && this.JobsWithinHorizon()
                && this.JobsNotOverlapping()
                && this.TransitionsExist()
                && this.ObjectiveCorrespondsWithStartTimes(objective);

            if (feasible)
            {
                this.Status = FeasibilityStatus.Feasible;
            }

            return this.Status;
        }

        private bool EveryJobHasStartTime()
        {
            foreach (var job in this.instance.Jobs)
            {
                if (!this.startTimes.ContainsJob(job))
                {
                    this.Status = FeasibilityStatus.JobHasNoStartTime;
                    this.Job = job;
                    return false;
                }
            }

            return true;
        }

        private bool JobsNotOverlapping()
        {
            foreach (var machineIndex in this.instance.Machines)
            {
                var orderedMachineJobs = this.instance.MachineJobs[machineIndex]
                    .OrderBy(job => this.startTimes[job]);
                foreach (var (job, nextJob) in orderedMachineJobs.SuccessionPairs())
                {
                    if ((this.startTimes[job] + job.ProcessingTime) > this.startTimes[nextJob])
                    {
                        this.Status = FeasibilityStatus.OverlappingOperations;
                        this.Job = job;
                        this.NextJob = nextJob;
                        this.Machine = machineIndex;
                        return false;
                    }
                }
            }
            
            return true;
        }

        private bool JobsWithinHorizon()
        {
            foreach (var job in this.instance.Jobs)
            {
                var startTime = this.startTimes[job];
                if (startTime < 0 || (startTime + job.ProcessingTime) > this.instance.Horizon())
                {
                    this.Status = FeasibilityStatus.JobOutsideHorizon;
                    this.Job = job;
                    return false;
                }
            }

            return true;
        }
        
        private bool TransitionsExist()
        {
            try
            {
                var cost = this.startTimes.TotalEnergyCost(this.instance);
                return true;
            }
            catch
            {
                this.Status = FeasibilityStatus.TransitionDoNotExist;
                return false;
            }
        }
        
        private bool ObjectiveCorrespondsWithStartTimes(int? objective)
        {
            if (!objective.HasValue)
            {
                return true;
            }
            
            var cost = this.startTimes.TotalEnergyCost(this.instance);
            
            // Relaxed, some solvers may not consider optimal switching between jobs.
            if (objective.Value < cost)
            {
                this.Status = FeasibilityStatus.ObjectiveNotCorrespondingWithStartTimes;
                return false;
            }
            
            return true;
        }

        public enum FeasibilityStatus
        {
            Unknown = 0,
            Feasible = 1,
            JobHasNoStartTime = 2,
            OverlappingOperations = 3,
            JobOutsideHorizon = 4,
            TransitionDoNotExist = 5,
            ObjectiveNotCorrespondingWithStartTimes = 6,
        }
    }
}
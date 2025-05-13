// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.DataStructs
{
    using System;
    using System.Linq;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;
    using Iirc.Utils.Collections;

    public static class StartTimesExtensions
    {
        public static int TotalEnergyCost(this StartTimes startTimes, ExtendedInstance instance)
        {
            int tec = 0;
            var orderedJobsOnMachines = startTimes.GetOrderedJobsOnMachines(instance);
            foreach (var machineIndex in instance.Machines)
            {
                var orderedJobs = orderedJobsOnMachines[machineIndex];

                if (!orderedJobs.Any())
                {
                    tec += instance.OptimalSwitchingCosts.First().Last().Value;
                    continue;
                }
                
                // Switching to first job.
                {
                    var startInterval = startTimes.StartInterval(orderedJobs.First(), instance);
                    tec += instance.OptimalSwitchingCosts.First()[startInterval.Index].Value;
                }
                
                // Switching between jobs.
                foreach (var (job, nextJob) in orderedJobs.SuccessionPairs())
                {
                    var completionInterval = startTimes.CompletionInterval(job, instance);
                    var startInterval = startTimes.StartInterval(nextJob, instance);
                    tec += instance.OptimalSwitchingCosts[completionInterval.Index + 1][startInterval.Index].Value;
                }
                
                // Switching after last job.
                {
                    var completionInterval = startTimes.CompletionInterval(orderedJobs.Last(), instance);
                    tec += instance.OptimalSwitchingCosts[completionInterval.Index + 1].Last().Value;
                }
                
                // Costs of processing the jobs.
                foreach (var job in instance.Jobs)
                {
                    var startInterval = startTimes.StartInterval(job, instance);
                    var completionInterval = startTimes.CompletionInterval(job, instance);
                    tec += instance.TotalEnergyCost(
                        startInterval.Index,
                        completionInterval.Index,
                        instance.OnPowerConsumption);
                }
            }

            return tec;
        }

        /// <summary>
        /// Gets the interval where the job starts. If the job starts at the boundary of two intervals, the latest one
        /// is returned.
        /// </summary>
        /// <param name="startTimes">The start times.</param>
        /// <param name="job">The job.</param>
        /// <param name="instance">The instance.</param>
        /// <returns>The interval where the job starts.</returns>
        public static Interval StartInterval(this StartTimes startTimes, Job job, Instance instance)
        {
            return instance.Intervals[startTimes[job] / instance.LengthInterval];
        }

        /// <summary>
        /// Gets the interval where the job completes. If the job completes at the boundary of two intervals, the
        /// earliest one is returned.
        /// </summary>
        /// <param name="startTimes">The start times.</param>
        /// <param name="job">The job.</param>
        /// <param name="instance">The instance.</param>
        /// <returns>The interval where the job completes.</returns>
        public static Interval CompletionInterval(this StartTimes startTimes, Job job, Instance instance)
        {
            var completionTime = startTimes[job] + job.ProcessingTime;
            if (completionTime % instance.LengthInterval == 0)
            {
                // On boundary.
                return instance.Intervals[(completionTime / instance.LengthInterval) - 1];
            }
            else
            {
                return instance.Intervals[completionTime / instance.LengthInterval];
            }
        }
    }
}
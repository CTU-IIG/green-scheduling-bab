// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.DataStructs
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;

    public class StartTimes : IEnumerable<KeyValuePair<Job, int>>
    {
        private Dictionary<Job, int> jobToStartTime;

        public StartTimes()
        {
            this.jobToStartTime = new Dictionary<Job, int>();
        }
        
        public StartTimes(Instance instance, List<IndexedStartTime> indexedStartTimes) : this()
        {
            if (indexedStartTimes == null)
            {
                return;
            }

            foreach (var indexedStartTime in indexedStartTimes)
            {
                var job = instance.Jobs[indexedStartTime.JobIndex];
                this.jobToStartTime[job] = indexedStartTime.StartTime;
            }
        }
        
        public StartTimes(StartTimes startTimes)
        {
            this.jobToStartTime = new Dictionary<Job, int>(startTimes);
        }
        
        public StartTimes(Dictionary<Job, int> startTimes)
        {
            this.jobToStartTime = startTimes;
        }

        IEnumerator<KeyValuePair<Job, int>> IEnumerable<KeyValuePair<Job, int>>.GetEnumerator()
        {
            return this.jobToStartTime.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.jobToStartTime.GetEnumerator();
        }

        public bool ContainsJob(Job job)
        {
            return this.jobToStartTime.ContainsKey(job);
        }

        public int this[Job job]
        {
            get { return this.jobToStartTime[job]; }
            set
            {
                 this.jobToStartTime[job] = value;
            }
        }
        
        public override string ToString()
        {
            var startTimesAsStr = string.Empty;
            foreach (var (job, startTime) in this.jobToStartTime)
            {
                if (startTimesAsStr != string.Empty)
                {
                    startTimesAsStr += Environment.NewLine;
                }

                startTimesAsStr += $"{job} -> {startTime}";
            }

            return startTimesAsStr;
        }
        
        public List<List<Job>> GetOrderedJobsOnMachines(Instance instance)
        {
            var orderedJobs = Enumerable
                .Range(0, instance.MachinesCount)
                .Select(_ => new List<Job>())
                .ToList();

            foreach (var machinePairs in this.jobToStartTime.GroupBy(pair => pair.Key.MachineIdx))
            {
                orderedJobs[machinePairs.Key] =
                    machinePairs.OrderBy(pair => pair.Value).Select(pair => pair.Key).ToList();
            }

            return orderedJobs;
        }
        
        public List<IndexedStartTime> ToIndexedStartTimes()
        {
            return this.jobToStartTime
                .Select(pair => new IndexedStartTime { JobIndex = pair.Key.Index, StartTime = pair.Value })
                .ToList();
        }
        
        /// <summary>
        /// A start time of a job.
        /// </summary>
        public struct IndexedStartTime
        {
            /// <summary>
            /// Gets or sets the index of a job.
            /// </summary>
            public int JobIndex { get; set; }
            
            /// <summary>
            /// Gets or sets the start time of the job.
            /// </summary>
            public int StartTime { get; set; }
        }
    }
}
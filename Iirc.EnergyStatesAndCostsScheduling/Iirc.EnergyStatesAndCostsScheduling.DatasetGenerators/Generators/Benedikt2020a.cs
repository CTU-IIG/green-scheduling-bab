// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.Generators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.Interface;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Interface;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.StateDiagrams;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The dataset generator proposed in [Benedikt2020a].
    /// </summary>
    public class Benedikt2020a : IDatasetGenerator
    {
        private static readonly int LengthInterval = 1;
        
        private Prescription prescription;
        private SpecializedPrescription specializedPrescription;
        private Random random;

        public IEnumerable<Instance> GenerateInstances(Prescription prescription, string prescriptionPath)
        {
            this.prescription = prescription;
            this.specializedPrescription = JObject
                .FromObject(prescription.SpecializedPrescription)
                .ToObject<SpecializedPrescription>();
            this.random = this.prescription.RandomSeed.HasValue ?
                new Random(this.prescription.RandomSeed.Value) : new Random();


            foreach (var stateDiagramKind in this.specializedPrescription.StateDiagrams)
            {
                IStateDiagram stateDiagram = null;
                switch (stateDiagramKind)
                {
                    case StateDiagramKind.Aghelinejad2017a:
                        stateDiagram = new Shared.StateDiagrams.Aghelinejad2017a();
                        break;
                    
                    case StateDiagramKind.Benedikt2020aTwosby:
                        stateDiagram = new Shared.StateDiagrams.Benedikt2020aTwosby();
                        break;
                }
                
                foreach (var repetition in Enumerable.Range(0, this.prescription.RepetitionsCount))
                {
                    foreach (var jobsCount in this.specializedPrescription.JobsCounts)
                    {
                        foreach (var maxProcessingTime in this.specializedPrescription.MaxProcessingTime)
                        {
                            foreach (var instance in this.GenerateInstances(
                                repetition: repetition,
                                jobsCount: jobsCount,
                                maxProcessingTime: maxProcessingTime,
                                stateDiagram: stateDiagram))
                            {
                                yield return instance;
                            }
                        }
                    }
                }
            }
        }

        public IEnumerable<Instance> GenerateInstances(
            int repetition,
            int jobsCount,
            int maxProcessingTime,
            IStateDiagram stateDiagram)
        {
            var jobs = new List<Job>();
            foreach (var jobIndex in Enumerable.Range(0, jobsCount))
            {
                var processingTime = this.random.Next(1, maxProcessingTime + 1);
                
                jobs.Add(new Job(
                    jobIndex,
                    jobIndex,
                    0,
                    processingTime));
            }

            // Generate all costs.
            var maxHorizonMultiplier = this.specializedPrescription.HorizonMultipliers.Max();
            int maxAvailableTime = (int)Math.Ceiling(jobs.Sum(job => job.ProcessingTime) * maxHorizonMultiplier);
            int minNonProcessingTime = 1 + stateDiagram.OffOnTime[0] + stateDiagram.OnOffTime[0] + 1;
            
            var allEnergyCosts = Enumerable
                .Range(0, maxAvailableTime + minNonProcessingTime)
                .Select(_ => this.random.Next(1, this.specializedPrescription.MaxEnergyCost + 1))
                .ToArray();

            foreach (var horizonMultiplier in this.specializedPrescription.HorizonMultipliers)
            {
                int availableTime = (int)Math.Ceiling(jobs.Sum(job => job.ProcessingTime) * horizonMultiplier);
                
                var intervals = Enumerable
                    .Range(0, availableTime + minNonProcessingTime)
                    .Select(intervalIdx =>
                        new Interval(intervalIdx, LengthInterval * intervalIdx, LengthInterval * (intervalIdx + 1), allEnergyCosts[intervalIdx]))
                    .ToArray();
                
                var metadata = new {
                    repetition,
                    jobsCount,
                    horizonMultiplier,
                    intervalsCount = intervals.Length,
                    stateDiagram = stateDiagram.GetType().Name
                };
                
                yield return new Instance(
                    machinesCount: 1,
                    jobs: jobs.ToArray(),
                    intervals: intervals,
                    lengthInterval: LengthInterval,
                    offOnTime: stateDiagram.OffOnTime,
                    onOffTime: stateDiagram.OnOffTime,
                    offOnPowerConsumption: stateDiagram.OffOnPowerConsumption,
                    onOffPowerConsumption: stateDiagram.OnOffPowerConsumption,
                    offIdleTime: stateDiagram.OffIdleTime,
                    idleOffTime: stateDiagram.IdleOffTime,
                    offIdlePowerConsumption: stateDiagram.OffIdlePowerConsumption,
                    idleOffPowerConsumption: stateDiagram.IdleOffPowerConsumption,
                    onPowerConsumption: stateDiagram.OnPowerConsumption,
                    idlePowerConsumption: stateDiagram.IdlePowerConsumption,
                    offPowerConsumption: stateDiagram.OffPowerConsumption,
                    metadata: metadata);
            }
        }

        private class SpecializedPrescription
        {
            /// <summary>
            /// Gets or sets the jobs count.
            /// </summary>
            public List<int> JobsCounts { get; set; }
            
            /// <summary>
            /// Gets or sets the horizon multipliers used to generate the number of intervals. The number of intervals
            /// is taken as: (sum of jobs processing times in an instance)*multiplier + minimum non-processing time.
            /// </summary>
            public List<double> HorizonMultipliers { get; set; }
            
            /// <summary>
            /// Gets or sets the maximum energy cost (inclusive). The energy costs are sampled from discrete uniform
            /// distribution [1, <see cref="MaxEnergyCost"/>].
            /// </summary>
            public int MaxEnergyCost { get; set; }
            
            /// <summary>
            /// Gets or sets the maximum processing time (inclusive). The processing times are sampled from discrete
            /// uniform distribution [1, <see cref="MaxProcessingTime"/>].
            /// </summary>
            public List<int> MaxProcessingTime { get; set; }
            
            /// <summary>
            /// Gets or sets the state diagrams.
            /// </summary>
            public List<StateDiagramKind> StateDiagrams { get; set; }
        }
    }
}
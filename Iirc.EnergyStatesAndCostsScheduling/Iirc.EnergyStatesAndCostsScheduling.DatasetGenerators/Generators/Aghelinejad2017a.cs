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
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The dataset generator proposed in [Aghelinejad2017a].
    /// </summary>
    public class Aghelinejad2017a : IDatasetGenerator
    {
        private static readonly int LengthInterval = 1;
        private IStateDiagram stateDiagram = new Shared.StateDiagrams.Aghelinejad2017a();
        
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

            foreach (var instanceSize in this.specializedPrescription.InstanceSizes)
            {
                foreach (var repetition in Enumerable.Range(0, this.prescription.RepetitionsCount))
                {
                    yield return this.GenerateInstance( 
                        instanceSize,
                        repetition);
                }
            }
        }

        public Instance GenerateInstance(
            InstanceSize instanceSize,
            int repetition)
        {
            var jobs = new List<Job>();
            foreach (var jobIndex in Enumerable.Range(0, instanceSize.JobsCount))
            {
                var processingTime = this.random.Next(1, this.specializedPrescription.MaxProcessingTime + 1);
                
                jobs.Add(new Job(
                    jobIndex,
                    jobIndex,
                    0,
                    processingTime));
            }

            var energyCosts = Enumerable
                .Range(0, instanceSize.IntervalsCount)
                .Select(_ => this.random.Next(1, this.specializedPrescription.MaxEnergyCost + 1))
                .ToArray();
            var intervals = Enumerable
                .Range(0, instanceSize.IntervalsCount)
                .Select(intervalIdx =>
                    new Interval(intervalIdx, LengthInterval * intervalIdx, LengthInterval * (intervalIdx + 1), energyCosts[intervalIdx]))
                .ToArray();

            var metadata = new {
                instanceSize.IntervalsCount,
                instanceSize.JobsCount,
                repetition
            };
            
            return new Instance(
                machinesCount: 1,
                jobs: jobs.ToArray(),
                intervals: intervals,
                lengthInterval: LengthInterval,
                offOnTime: this.stateDiagram.OffOnTime,
                onOffTime: this.stateDiagram.OnOffTime,
                offOnPowerConsumption: this.stateDiagram.OffOnPowerConsumption,
                onOffPowerConsumption: this.stateDiagram.OnOffPowerConsumption,
                offIdleTime: this.stateDiagram.OffIdleTime,
                idleOffTime: this.stateDiagram.IdleOffTime,
                offIdlePowerConsumption: this.stateDiagram.OffIdlePowerConsumption,
                idleOffPowerConsumption: this.stateDiagram.IdleOffPowerConsumption,
                onPowerConsumption: this.stateDiagram.OnPowerConsumption,
                idlePowerConsumption: this.stateDiagram.IdlePowerConsumption,
                offPowerConsumption: this.stateDiagram.OffPowerConsumption,
                metadata: metadata);
        }

        private class SpecializedPrescription
        {
            /// <summary>
            /// Gets or sets the instance sizes.
            /// </summary>
            public InstanceSize[] InstanceSizes { get; set; }
            
            /// <summary>
            /// Gets or sets the maximum energy cost (inclusive). The energy costs are sampled from discrete uniform
            /// distribution [1, <see cref="MaxEnergyCost"/>].
            /// </summary>
            public int MaxEnergyCost { get; set; }
            
            /// <summary>
            /// Gets or sets the maximum processing time (inclusive). The processing times are sampled from discrete
            /// uniform distribution [1, <see cref="MaxProcessingTime"/>].
            /// </summary>
            public int MaxProcessingTime { get; set; }
        }

        /// <summary>
        /// The representation of the instance size.
        /// </summary>
        public class InstanceSize
        {
            /// <summary>
            /// Gets or sets the number of the jobs.
            /// </summary>
            public int JobsCount { get; set; }
            
            /// <summary>
            /// Gets or sets the number of the intervals.
            /// </summary>
            public int IntervalsCount { get; set; }
        }
    }
}
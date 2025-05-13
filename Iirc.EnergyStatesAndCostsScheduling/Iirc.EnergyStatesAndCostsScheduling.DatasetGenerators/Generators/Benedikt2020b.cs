// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.Generators
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.EnergyCosts;
    using Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.Interface;
    using Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.ProcessingTimes;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Interface;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.StateDiagrams;
    using Iirc.Utils.Collections;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// The dataset generator proposed in [Benedikt2020b].
    /// </summary>
    public class Benedikt2020b : IDatasetGenerator
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
            this.random = this.prescription.RandomSeed.HasValue
                ? new Random(this.prescription.RandomSeed.Value)
                : new Random();

            foreach (var jobsCount in this.specializedPrescription.JobsCounts)
            {
                foreach (var repetition in Enumerable.Range(0, this.prescription.RepetitionsCount))
                {
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

                        foreach (var energyCostsProviderRaw in this.specializedPrescription.EnergyCostsProviders)
                        {
                            IEnergyCostsProvider energyCostsProvider = null;
                            switch (energyCostsProviderRaw.Name)
                            {
                                case EnergyCostsProviderKind.Uniform:
                                {
                                    var nativeConfig = JObject
                                        .FromObject(energyCostsProviderRaw.Config)
                                        .ToObject<UniformEnergyCostsProvider.Config>();
                                    energyCostsProvider =
                                        UniformEnergyCostsProvider.FromConfig(nativeConfig, this.random);
                                    break;
                                }

                                case EnergyCostsProviderKind.File:
                                {
                                    var nativeConfig = JObject
                                        .FromObject(energyCostsProviderRaw.Config)
                                        .ToObject<FileEnergyCostsProvider.Config>();
                                    var energyCostsDir = Path.Combine(
                                        Path.GetDirectoryName(prescriptionPath), "energy-costs");
                                    energyCostsProvider = FileEnergyCostsProvider.FromConfig(
                                        nativeConfig,
                                        energyCostsDir);
                                    break;
                                }
                            }

                            foreach (var processingTimesProviderRaw in this.specializedPrescription
                                .ProcessingTimesProviders)
                            {
                                IProcessingTimesProvider processingTimesProvider = null;
                                switch (processingTimesProviderRaw.Name)
                                {
                                    case ProcessingTimesProviderKind.Uniform:
                                    {
                                        var nativeConfig = JObject
                                            .FromObject(processingTimesProviderRaw.Config)
                                            .ToObject<UniformProcessingTimesProvider.Config>();
                                        processingTimesProvider = UniformProcessingTimesProvider.FromConfig(
                                            nativeConfig,
                                            this.random);
                                        break;
                                    }

                                    case ProcessingTimesProviderKind.Groups:
                                    {
                                        var nativeConfig = JObject
                                            .FromObject(processingTimesProviderRaw.Config)
                                            .ToObject<GroupsProcessingTimesProvider.Config>();
                                        processingTimesProvider = GroupsProcessingTimesProvider.FromConfig(
                                            nativeConfig,
                                            this.random);
                                        break;
                                    }
                                }

                                foreach (var instance in this.GenerateInstances(
                                    repetition: repetition,
                                    jobsCount: jobsCount,
                                    processingTimesProvider: processingTimesProvider,
                                    energyCostsRepeatCounts: energyCostsProviderRaw.RepeatCount,
                                    hopsCounts: energyCostsProviderRaw.HopsCount,
                                    hopCostProvider: energyCostsProviderRaw.HopCostProvider == null ?
                                        null
                                        : new UniformEnergyCostsProvider(
                                            energyCostsProviderRaw.HopCostProvider.MinEnergyCost,
                                            energyCostsProviderRaw.HopCostProvider.MaxEnergyCost,
                                            this.random),
                                    stateDiagram: stateDiagram,
                                    energyCostsProvider: energyCostsProvider))
                                {
                                    yield return instance;
                                }
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<Instance> GenerateInstances(
            int repetition,
            int jobsCount,
            IProcessingTimesProvider processingTimesProvider,
            List<int> energyCostsRepeatCounts,
            List<int> hopsCounts,
            UniformEnergyCostsProvider hopCostProvider,
            IStateDiagram stateDiagram,
            IEnergyCostsProvider energyCostsProvider)
        {
            int minRepeatCount = energyCostsRepeatCounts.Min();

            // All time-related values are related to the smallest repeat count - normalized values.
            var normProcessingTimes = Enumerable
                .Range(0, jobsCount)
                .Select(_ => processingTimesProvider.Generate())
                .ToList();

            // Generate all costs.
            var maxHorizonMultiplier = this.specializedPrescription.HorizonMultipliers.Max();
            int normMaxAvailableTime = (int) Math.Ceiling(normProcessingTimes.Sum() * maxHorizonMultiplier);
            int normMinNonProcessingTime = 1 + stateDiagram.OffOnTime[0] + stateDiagram.OnOffTime[0] + 1;
            int normAllIntervalsCount = normMaxAvailableTime + normMinNonProcessingTime;
            var allEnergyCosts = energyCostsProvider.Generate(normAllIntervalsCount, minRepeatCount);

            // Non-positive costs are set to 1.
            allEnergyCosts = allEnergyCosts.Select(c => Math.Max(1, c)).ToList();

            foreach (var horizonMultiplier in this.specializedPrescription.HorizonMultipliers)
            {
                int normAvailableTime = (int) Math.Ceiling(normProcessingTimes.Sum() * horizonMultiplier);

                var normIntervals = Enumerable
                    .Range(0, normAvailableTime + normMinNonProcessingTime)
                    .Select(intervalIdx =>
                        new Interval(intervalIdx, LengthInterval * intervalIdx, LengthInterval * (intervalIdx + 1),
                            allEnergyCosts[intervalIdx]))
                    .ToArray();

                foreach (var repeatCount in energyCostsRepeatCounts)
                {
                    // All time-related values are multiplied by timeMul.
                    int timeMul = repeatCount / minRepeatCount;

                    var intervals = new List<Interval>();
                    int intervalIdx = 0;
                    foreach (var normInterval in normIntervals)
                    {
                        for (int i = 0; i < timeMul; i++)
                        {
                            intervals.Add(new Interval(intervalIdx, intervalIdx, intervalIdx + 1,
                                normInterval.EnergyCost));
                            intervalIdx++;
                        }
                    }

                    foreach (var hopsCount in hopsCounts)
                    {
                        // Drop costs.
                        var instanceIntervals = intervals.ToList();
                        {
                            var hopIndices = instanceIntervals.Select(interval => interval.Index)
                                .ToList()
                                .Shuffle(this.random)
                                .Take(hopsCount)
                                .ToList();
                            foreach (var hopIdx in hopIndices)
                            {
                                instanceIntervals[hopIdx] = new Interval(hopIdx, hopIdx, hopIdx + 1, hopCostProvider.Generate(1, 1)[0]);
                            }
                        }
                        
                        var jobs = new List<Job>();
                        foreach (var jobIndex in Enumerable.Range(0, jobsCount))
                        {
                            var processingTime = normProcessingTimes[jobIndex] * timeMul;

                            jobs.Add(new Job(
                                jobIndex,
                                jobIndex,
                                0,
                                processingTime));
                        }

                        var metadata = new
                        {
                            repetition,
                            jobsCount,
                            horizonMultiplier,
                            intervalsCount = instanceIntervals.Count,
                            stateDiagram = stateDiagram.GetType().Name,
                            repeatCount,
                            hopsCount,
                            hopCostProvider = hopCostProvider?.GetConfig(),
                            energyCostsProvider = energyCostsProvider.GetType().Name,
                            energyCostsConfig = energyCostsProvider.GetConfig(),
                            processingTimesProvider = processingTimesProvider.GetType().Name,
                            processingTimesConfig = processingTimesProvider.GetConfig()
                        };

                        yield return new Instance(
                            machinesCount: 1,
                            jobs: jobs.ToArray(),
                            intervals: instanceIntervals.ToArray(),
                            lengthInterval: LengthInterval,
                            offOnTime: MulValues(stateDiagram.OffOnTime, timeMul),
                            onOffTime: MulValues(stateDiagram.OnOffTime, timeMul),
                            offOnPowerConsumption: stateDiagram.OffOnPowerConsumption,
                            onOffPowerConsumption: stateDiagram.OnOffPowerConsumption,
                            offIdleTime: MulValues(stateDiagram.OffIdleTime, timeMul),
                            idleOffTime: MulValues(stateDiagram.IdleOffTime, timeMul),
                            offIdlePowerConsumption: stateDiagram.OffIdlePowerConsumption,
                            idleOffPowerConsumption: stateDiagram.IdleOffPowerConsumption,
                            onPowerConsumption: stateDiagram.OnPowerConsumption,
                            idlePowerConsumption: stateDiagram.IdlePowerConsumption,
                            offPowerConsumption: stateDiagram.OffPowerConsumption,
                            metadata: metadata);
                    }
                }
            }
        }

        private int[] MulValues(int[] values, int mul)
        {
            return values?.Select(v => v * mul).ToArray();
        }

        private int?[] MulValues(int?[] values, int mul)
        {
            return values?.Select(v => v == null ? null : v * mul).ToArray();
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
            /// Gets or sets the energy costs providers.
            /// </summary>
            public List<EnergyCostsProvider> EnergyCostsProviders { get; set; }

            /// <summary>
            /// Gets or sets the processing times providers.
            /// </summary>
            public List<ProcessingTimesProvider> ProcessingTimesProviders { get; set; }

            /// <summary>
            /// Gets or sets the state diagrams.
            /// </summary>
            public List<StateDiagramKind> StateDiagrams { get; set; }
        }

        /// <summary>
        /// The energy cost provider.
        /// </summary>
        private class EnergyCostsProvider
        {
            /// <summary>
            /// Gets or sets the name of the energy costs provider.
            /// </summary>
            public EnergyCostsProviderKind Name { get; set; }

            /// <summary>
            /// Gets or sets the repeat counts. Each repeat count represents the number of consecutive costs that are
            /// the same. This is useful for changing the granularity of the scheduling horizon (e.g., for hourly RTP
            /// pricing and 1 minute scheduling granularity, set the repeat count to 60).
            /// </summary>
            public List<int> RepeatCount { get; set; }
            
            /// <summary>
            /// Gets or sets the number of random hops in the energy costs within the horizon.
            /// </summary>
            public List<int> HopsCount { get; set; }
            
            /// <summary>
            /// Gets or sets the energy cost provider for hops.
            /// </summary>
            public UniformEnergyCostsProvider.Config HopCostProvider { get; set; }

            /// <summary>
            /// Gets the specific configuration of the energy cost provider.
            /// </summary>
            public JObject Config { get; set; }
        }

        /// <summary>
        /// The processing times provider.
        /// </summary>
        private class ProcessingTimesProvider
        {
            /// <summary>
            /// Gets or sets the name of the processing times provider.
            /// </summary>
            public ProcessingTimesProviderKind Name { get; set; }

            /// <summary>
            /// Gets the specific configuration of the processing times provider.
            /// </summary>
            public JObject Config { get; set; }
        }
    }
}
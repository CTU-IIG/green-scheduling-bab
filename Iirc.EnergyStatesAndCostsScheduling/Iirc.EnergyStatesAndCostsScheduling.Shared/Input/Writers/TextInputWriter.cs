// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Input.Writers
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input.Readers;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Interface.Input;
    using Newtonsoft.Json;

    public class TextInputWriter : IInputWriter
    {
        private static readonly int NoValue = -1;
            
        public void WriteToPath(Instance instance, string instancePath)
        {
            throw new NotImplementedException($"{nameof(TextInputWriter)} supports only ExtendedInstance.");
        }

        public void WriteToPath(ExtendedInstance instance, string instancePath)
        {
            using (var stream = new StreamWriter(instancePath))
            {
                this.WriteMachinesCount(stream, instance);
                this.WriteJobs(stream, instance);
                this.WriteIntervals(stream, instance);
                this.WriteLengthInterval(stream, instance);
                this.WriteOnPowerConsumption(stream, instance);
                this.WriteEarliestOnIntervalIdx(stream, instance);
                this.WriteLatestOnIntervalIdx(stream, instance);
                this.WriteOptimalSwitchingCosts(stream, instance, instance.OptimalSwitchingCosts);
                this.WriteOptimalSwitchingCosts(stream, instance, instance.FullOptimalSwitchingCosts);
                this.WriteCumulativeEnergyCost(stream, instance);
            }
        }

        private void WriteMachinesCount(StreamWriter stream, ExtendedInstance instance)
        {
            stream.WriteLine(instance.MachinesCount);
        }
        
        private void WriteJobs(StreamWriter stream, ExtendedInstance instance)
        {
            stream.WriteLine(instance.Jobs.Length);
            foreach (var job in instance.Jobs)
            {
                stream.WriteLine($"{job.Id} {job.Index} {job.MachineIdx} {job.ProcessingTime}");
            }
        }
        
        private void WriteIntervals(StreamWriter stream, ExtendedInstance instance)
        {
            stream.WriteLine(instance.Intervals.Length);
            foreach (var interval in instance.Intervals)
            {
                stream.WriteLine($"{interval.Index} {interval.Start} {interval.End} {interval.EnergyCost}");
            }
        }

        private void WriteLengthInterval(StreamWriter stream, ExtendedInstance instance)
        {
            stream.WriteLine(instance.LengthInterval);
        }
        
        private void WriteOnPowerConsumption(StreamWriter stream, ExtendedInstance instance)
        {
            stream.WriteLine(instance.OnPowerConsumption);
        }
        
        private void WriteEarliestOnIntervalIdx(StreamWriter stream, ExtendedInstance instance)
        {
            stream.WriteLine(instance.EarliestOnIntervalIdx);
        }
        
        private void WriteLatestOnIntervalIdx(StreamWriter stream, ExtendedInstance instance)
        {
            stream.WriteLine(instance.LatestOnIntervalIdx);
        }
        
        private void WriteOptimalSwitchingCosts(
            StreamWriter stream,
            ExtendedInstance instance,
            int?[][] optimalSwitchingCosts)
        {
            int rowsCount = optimalSwitchingCosts.Length;
            int colsCount = optimalSwitchingCosts[0].Length;
            stream.WriteLine($"{rowsCount} {colsCount}");
            for (int row = 0; row < rowsCount; row++)
            {
                for (int col = 0; col < colsCount; col++)
                {
                    if (col > 0)
                    {
                        stream.Write(' ');
                    }

                    var value = optimalSwitchingCosts[row][col];
                    if (value.HasValue)
                    {
                        stream.Write(value.Value);
                    }
                    else
                    {
                        stream.Write(NoValue);
                    }
                }
                
                stream.WriteLine();
            }
        }
        
        private void WriteCumulativeEnergyCost(StreamWriter stream, ExtendedInstance instance)
        {
            int rowsCount = instance.CumulativeEnergyCost.Length;
            int colsCount = instance.CumulativeEnergyCost[0].Length;
            stream.WriteLine($"{rowsCount} {colsCount}");
            for (int row = 0; row < rowsCount; row++)
            {
                for (int col = 0; col < colsCount; col++)
                {
                    if (col > 0)
                    {
                        stream.Write(' ');
                    }

                    var value = instance.CumulativeEnergyCost[row][col];
                    stream.Write(value);
                }
                
                stream.WriteLine();
            }
        }
    }
}

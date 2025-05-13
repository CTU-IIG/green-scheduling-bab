// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Input.Writers
{
    using System.IO;
    using System.Linq;
    using System.Runtime.Serialization.Formatters.Binary;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input.Readers;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Interface.Input;
    using Newtonsoft.Json;

    public class JsonInputWriter : IInputWriter
    {
        public void WriteToPath(Instance instance, string instancePath)
        {
            File.WriteAllText(instancePath, JsonConvert.SerializeObject(ToJsonInstance(instance)));
        }
        
        public void WriteToPath(ExtendedInstance instance, string instancePath)
        {
            this.WriteToPath((Instance)instance, instancePath);
        }

        public static JsonInstance ToJsonInstance(Instance instance, bool serializeExtendedInstance = true)
        {
            var jsonJobs = instance.Jobs
                .Select(job => new JsonJob
                {
                    Id = job.Id,
                    MachineIdx = job.MachineIdx,
                    ProcessingTime =  job.ProcessingTime
                })
                .ToArray();

            byte[] serializedExtendedInstance = null;
            if (serializeExtendedInstance)
            {
                if (instance.SerializedExtendedInstance != null)
                {
                     serializedExtendedInstance = instance.SerializedExtendedInstance;
                }
                else
                {
                    var extendedInstance = ExtendedInstance.GetExtendedInstance(instance);
                    extendedInstance.Metadata = null;    // Due to metadata possibly not serializable.
                    extendedInstance.GenerateFullExtendedInstance();
                    
                    var stream = new MemoryStream(2048);
                    using (stream)
                    {
                        var bf = new BinaryFormatter();
                        bf.Serialize(stream, extendedInstance);
                    }

                    serializedExtendedInstance = stream.ToArray();
                }
            }
            
            return new JsonInstance
            {
                MachinesCount = instance.MachinesCount,
                Jobs = jsonJobs,
                EnergyCosts = instance.Intervals.Select(interval => interval.EnergyCost).ToArray(),
                LengthInterval = instance.LengthInterval,
                OffOnTime = instance.OffOnTime,
                OnOffTime = instance.OnOffTime,
                OffOnPowerConsumption = instance.OffOnPowerConsumption,
                OnOffPowerConsumption = instance.OnOffPowerConsumption,
                OffIdleTime = instance.OffIdleTime,
                IdleOffTime = instance.IdleOffTime,
                OffIdlePowerConsumption = instance.OffIdlePowerConsumption,
                IdleOffPowerConsumption = instance.IdleOffPowerConsumption,
                OnPowerConsumption = instance.OnPowerConsumption,
                IdlePowerConsumption = instance.IdlePowerConsumption,
                OffPowerConsumption = instance.OffPowerConsumption,
                SerializedExtendedInstance = serializedExtendedInstance,
                Metadata = instance.Metadata,
                TimeForExtendedInstance = instance.TimeForExtendedInstance
            };
        }
    }
}

// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using CommandLine;
    using Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators.Interface;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input.Writers;
    using Newtonsoft.Json;

    class Program
    {
        public static int Main(string[] args)
        {
            return new Parser(parserConfig => parserConfig.HelpWriter = Console.Out)
                .ParseArguments<CmdOptions>(args)
                .MapResult(opts => Run(opts), errs => 1);
        }

        private static int Run(CmdOptions opts)
        {
            var prescriptionPath = DatasetGeneratorPrescriptionPath(opts);
            if (!File.Exists(prescriptionPath))
            {
                Console.WriteLine($"Dataset generator prescription file {prescriptionPath} does not exist.");
                return 1;
            }

            var prescription = JsonConvert.DeserializeObject<Prescription>(
                File.ReadAllText(prescriptionPath),
                new JsonSerializerSettings()
                {
                    DefaultValueHandling = DefaultValueHandling.Populate
                });

            var datasetsPath = Program.DatasetsPath(opts);
            if (!Directory.Exists(datasetsPath))
            {
                Directory.CreateDirectory(datasetsPath);
            }

            var allDatasetGeneratorTypes = GetDatasetGeneratorTypes();
            Type datasetGeneratorType;
            if (allDatasetGeneratorTypes.TryGetValue(prescription.DatasetGeneratorName, out datasetGeneratorType) == false)
            {
                Console.WriteLine($"Dataset generator {prescription.DatasetGeneratorName} does not exist.");
                return 1;
            }

            var datasetGenerator = (IDatasetGenerator) Activator.CreateInstance(datasetGeneratorType);

            var datasetName = Path.GetFileNameWithoutExtension(opts.PrescriptionFile);
            var datasetPath = Program.DatasetPath(opts, datasetName);

            if (Directory.Exists(datasetPath))
            {
                Directory.Delete(datasetPath, true);
            }
            Directory.CreateDirectory(datasetPath);

            var instanceWriter = new JsonInputWriter();
            var instanceIndex = 0;
            
            foreach (var instance in datasetGenerator.GenerateInstances(prescription, prescriptionPath))
            {
                var instanceChecker = new InstanceChecker();
                var instanceStatus = instanceChecker.Check(instance);
                if (instanceStatus != InstanceChecker.InstanceStatus.Ok)
                {
                    Console.WriteLine($"Instance check: {instanceStatus}");
                    return 1;
                }

                instance.TimeForExtendedInstance = TimeForExtendedInstance(instance);

                instanceWriter.WriteToPath(
                    instance,
                    Path.Combine(datasetPath, $"{instanceIndex}.json"));
                instanceIndex++;
            }
            
            Console.WriteLine($"Dataset {opts.PrescriptionFile} generated.");

            return 0;
        }

        private static TimeSpan TimeForExtendedInstance(Instance instance)
        {
            var sw = new Stopwatch();
            sw.Restart();
            var extendedInstance = new ExtendedInstance(instance);
            extendedInstance.GenerateFullExtendedInstance();
            sw.Stop();
            return sw.Elapsed;
        }

        private static Dictionary<string, Type> GetDatasetGeneratorTypes()
        {
            var datasetGeneratorInterfaceType = typeof(IDatasetGenerator);
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => datasetGeneratorInterfaceType.IsAssignableFrom(type))
                .Where(type => type.IsClass)
                .ToDictionary(type => type.Name, type => type);
        }
        
        public static string DatasetsPath(CmdOptions opts)
        {
            return Path.Combine(opts.DataRootPath, "datasets");
        }
        
        public static string DatasetGeneratorsPrescriptionPath(CmdOptions opts)
        {
            return Path.Combine(opts.DataRootPath, "dataset-generators-prescriptions");
        }
        
        public static string DatasetPath(CmdOptions opts, string datasetName)
        {
            return Path.Combine(Program.DatasetsPath(opts), datasetName);
        }
        
        public static string DatasetGeneratorPrescriptionPath(CmdOptions opts)
        {
            return Path.Combine(Program.DatasetGeneratorsPrescriptionPath(opts), opts.PrescriptionFile);
        }
    }
}

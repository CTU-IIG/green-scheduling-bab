// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Experiments
{
    using CommandLine;

    public class CmdOptions
    {
        [Option("num-threads", Default = 1, HelpText = "Number of threads solving instances in parallel.")]
        public int NumThreads { get; set; }

        [Option("from-scratch", HelpText = "Whether to start the experiment from scratch or to keep the old results.")]
        public bool FromScratch { get; set; }
        
        [Value(0, Required = true, MetaValue = "DATA_ROOT_PATH", HelpText = "Path to the root data directory.")]
        public string DataRootPath { get; set; }

        [Value(1, Required = true, MetaValue = "PRESCRIPTION_FILE", HelpText = "File name of the prescription file (with suffix).")]
        public string PrescriptionFile { get; set; }
    }
}
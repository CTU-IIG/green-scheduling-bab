// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.DatasetGenerators
{
    using CommandLine;

    public class CmdOptions
    {
        [Value(0, Required = true, MetaValue = "DATA_ROOT_PATH", HelpText = "Path to the root data directory.")]
        public string DataRootPath { get; set; }

        [Value(1, Required = true, MetaValue = "PRESCRIPTION_FILE", HelpText = "File name of the prescription file (with suffix).")]
        public string PrescriptionFile { get; set; }
    }
}

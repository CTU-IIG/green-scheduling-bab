// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.SolverCli
{
    using CommandLine;

    public class CmdOptions
    {
        [Value(0, Required = true, MetaValue = "CONFIG_PATH", HelpText = "Path to the configuration file.")]
        public string ConfigPath { get; set; }

        [Value(1, Required = true, MetaValue = "INSTANCE_PATH", HelpText = "Path to the instance file.")]
        public string InstancePath { get; set; }
    }
}
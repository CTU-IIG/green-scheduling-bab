// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Tests.Result
{
    using System.IO;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.DataStructs;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input.Readers;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Output;
    using Newtonsoft.Json;
    using Xunit;
    
    public class ResultTests
    {
        [Theory]
        [InlineData("aghelinejad2019a_tab1.json", "aghelinejad2019a_tab1.json", 155)]
        [InlineData("aghelinejad2019a_fig2.json", "aghelinejad2019a_fig2.json", 222)]
        public void StartTimesTotalEnergyCostTheory(string instanceName, string resultName, int expectedTec)
        {
            var instance = new InputReader().ReadFromPath(Path.Combine("instances", instanceName));
            var extendedInstance = ExtendedInstance.GetExtendedInstance(instance);
            extendedInstance.ComputeOptimalSwitchingCosts();
            extendedInstance.ComputeGapsLowerBounds();

            var result = JsonConvert.DeserializeObject<Result>(File.ReadAllText(Path.Combine("results", resultName)));
            var startTimes = new StartTimes(instance, result.StartTimes);
            
            Assert.Equal(expectedTec, startTimes.TotalEnergyCost(extendedInstance));
        }        
    }
}
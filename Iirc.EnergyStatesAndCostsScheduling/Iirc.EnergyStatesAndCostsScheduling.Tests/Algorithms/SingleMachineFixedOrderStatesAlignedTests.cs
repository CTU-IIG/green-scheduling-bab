// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Tests.Algorithms
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Algorithms;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.DataStructs;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input.Readers;
    using Iirc.Utils.SolverFoundations;
    using Xunit;

    public class SingleMachineFixedOrderStatesAlignedTests
    {
        [Theory]
        [InlineData("aghelinejad2019a_tab1.json", new [] { 0, 1, 2 }, new [] { 3, 5, 6 } )]
        [InlineData("aghelinejad2019a_tab1.json", new [] { 1, 2, 0 }, new [] { 3, 4, 6 } )]
        [InlineData("aghelinejad2019a_fig2.json", new [] { 0, 1, 2, 3, 4 }, new [] { 6, 9, 11, 21, 23 } )]
        [InlineData("fullhorizon.json", new [] { 0, 1 }, new [] { 3, 4 } )]
        [InlineData("fullhorizon.json", new [] { 1, 0 }, new [] { 3, 5 } )]
        public void SingleMachineFixedOrderStatesAlignedTheory(
            string instanceName,
            int[] orderedJobIds,
            int[] expectedStartTimesIds)
        {
            var instance = new InputReader().ReadFromPath(Path.Combine("instances", instanceName));
            var extendedInstance = ExtendedInstance.GetExtendedInstance(instance);
            extendedInstance.ComputeOptimalSwitchingCosts();
            extendedInstance.ComputeGapsLowerBounds();

            var orderedJobs = new List<Job>();
            var expectedStartTimes = new StartTimes();
            for (var jobPosition = 0; jobPosition < orderedJobIds.Length; jobPosition++)
            {
                var jobId = orderedJobIds[jobPosition];
                var expectedStartTime = expectedStartTimesIds[jobPosition];
                var selectedJob = extendedInstance.Jobs.Single(job => job.Id == jobId);
                
                orderedJobs.Add(selectedJob);
                expectedStartTimes[selectedJob] = expectedStartTime;
            }

            var alg = new FixedPermCostComputation(extendedInstance);
            alg.SetInput(orderedJobs);
            var status = alg.Solve();
            
            Assert.Equal(Status.Optimal, status);
            
            foreach (var job in orderedJobs)
            {
                Assert.Equal(expectedStartTimes[job], alg.StartTimes[job]);
            }
        }        
    }
}
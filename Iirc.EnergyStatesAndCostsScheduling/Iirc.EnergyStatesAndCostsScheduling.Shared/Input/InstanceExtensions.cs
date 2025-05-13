// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Input
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class InstanceExtensions
    {
        public static int TotalEnergyCost(
            this ExtendedInstance instance, int fromIntervalIndex, int toIntervalIndex, int powerConsumption)
        {
            return InstanceExtensions.TotalEnergyCost(
                fromIntervalIndex,
                toIntervalIndex,
                powerConsumption,
                instance.LengthInterval,
                instance.CumulativeEnergyCost);
        }
        
        public static int TotalEnergyCost(
            int fromIntervalIndex, int toIntervalIndex, int powerConsumption, int lengthInterval, int[][] cumulativeEnergyCost)
        {
            if (toIntervalIndex < fromIntervalIndex)
            {
                return 0;
            }
            
            var energyConsumptionPerInterval = lengthInterval * powerConsumption;
            return energyConsumptionPerInterval * cumulativeEnergyCost[fromIntervalIndex][toIntervalIndex];
        }

        public static int Horizon(this Instance instance)
        {
            return instance.Intervals.Last().End;
        }

        public static IEnumerable<Tuple<int, int>> StatePairsWithTransition(this ExtendedInstance instance)
        {
            foreach (var fromStateIdx in instance.StateInds)
            {
                foreach (var toStateIdx in instance.StateInds)
                {
                    if (instance.StateDiagramPowerConsumption[fromStateIdx][toStateIdx].HasValue)
                    {
                        yield return new Tuple<int, int>(fromStateIdx, toStateIdx);
                    }
                }
            }
        }
    }
}
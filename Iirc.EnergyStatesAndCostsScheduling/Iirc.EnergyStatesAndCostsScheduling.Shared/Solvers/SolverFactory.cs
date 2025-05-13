// This file is released under MIT license.
// See file LICENSE.txt for more information.

namespace Iirc.EnergyStatesAndCostsScheduling.Shared.Solvers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Iirc.EnergyStatesAndCostsScheduling.Shared.Input;
    using Iirc.Utils.SolverFoundations;

    public class SolverFactory
    {
        public ISolver<Instance, SolverConfig, SolverResult> Create(string solverName)
        {
            var solverTypes = GetSolverTypes();

            Type solverType;
            if (solverTypes.TryGetValue(solverName, out solverType) == false)
            {
                throw new SolverNotFoundException(solverName);
            }

            return (ISolver<Instance, SolverConfig, SolverResult>) Activator.CreateInstance(solverType);
        }

        private static Dictionary<string, Type> GetSolverTypes()
        {
            var solverInterfaceType = typeof(ISolver<Instance, SolverConfig, SolverResult>);
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes())
                .Where(type => solverInterfaceType.IsAssignableFrom(type))
                .Where(type => type.IsClass)
                .ToDictionary(type => type.Name, type => type);
        }

        public class SolverNotFoundException : Exception
        {
            public SolverNotFoundException()
            {

            }

            public SolverNotFoundException(string solverName) : base($"Solver {solverName} does not exist.")
            {

            }
        }
    }
}
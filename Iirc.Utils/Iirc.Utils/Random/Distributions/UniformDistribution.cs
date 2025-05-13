// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="UniformDistribution.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.Utils.Random.Distributions
{
    using System;
    
    public class UniformDistribution
    {
        private readonly double minValueInclusive;
        private readonly double maxValueExclusive;
        private readonly Random random;

        public UniformDistribution(double minValueInclusive, double maxValueExclusive, Random random)
        {
            this.minValueInclusive = minValueInclusive;
            this.maxValueExclusive = maxValueExclusive;
            this.random = random ?? new Random();
        }

        public double Sample()
        {
            var randValue = this.random.NextDouble();
            return randValue * (maxValueExclusive - minValueInclusive) + minValueInclusive;
        }
    }
}
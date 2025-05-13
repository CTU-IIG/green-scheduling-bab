// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="NumericComparer.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.Utils.Math
{
    using System;

    public class NumericComparer
    {
        public const double DefaultTolerance = 1e-08;

        public double Tolerance { get; }

        public static readonly NumericComparer Default = new NumericComparer(DefaultTolerance);

        public NumericComparer(double tolerance)
        {
            this.Tolerance = tolerance;
        }

        public bool AreEqual(double x, double y)
        {
            return Math.Abs(x - y) <= this.Tolerance;
        }

        public bool LessOrEqual(double x, double y)
        {
            return x < y || this.AreEqual(x, y);
        }

        public bool Greater(double x, double y)
        {
            return x > y && this.AreEqual(x, y) == false;
        }

        public bool Less(double x, double y)
        {
            return x < y && this.AreEqual(x, y) == false;
        }
    }
}
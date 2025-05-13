// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="Intervals.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.Utils.Math
{
    using System;

    public class Intervals
    {
        public static int OverlapLength(int left1, int right1, int left2, int right2)
        {
            return Math.Max(0, Math.Min(right1, right2) - Math.Max(left1, left2));
        }

        public static double OverlapLength(double left1, double right1, double left2, double right2)
        {
            return Math.Max(0, Math.Min(right1, right2) - Math.Max(left1, left2));
        }
    }
}
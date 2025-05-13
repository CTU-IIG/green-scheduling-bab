// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensions.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.Utils.Text
{
    using System;
    using System.Linq;

    public static class StringExtensions
    {
        public static string SanitizeWhitespace(this string str)
        {
            var lines = str
                .SplitNewlines()
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .Select(line => string.Join(' ', line.Split(' ', StringSplitOptions.RemoveEmptyEntries)));

            return string.Join("\n", lines);
        }

        public static string[] SplitNewlines(this string str)
        {
            return str.Split(new[] {"\r\n", "\r", "\n"}, StringSplitOptions.None);
        }

        public static string FirstToLower(this string str)
        {
            return string.IsNullOrEmpty(str) ? str : (char.ToLower(str[0]) + str.Substring(1));
        }
        
        public static string FirstToUpper(this string str)
        {
            return string.IsNullOrEmpty(str) ? str : (char.ToUpper(str[0]) + str.Substring(1));
        }
    }
}
// ---------------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.cs" company="Czech Technical University in Prague">
//   Copyright (c) 2018 Czech Technical University in Prague
// </copyright>
// ---------------------------------------------------------------------------------------------------------------------

namespace Iirc.Utils.Text
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Xunit;
    using Iirc.Utils.Collections;

    public class StringExtensionsTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData(" ", " ")]
        [InlineData(" Foo", " Foo")]
        [InlineData("Foo", "foo")]
        [InlineData("FooBar", "fooBar")]
        public void FirstToLowerTheory(string input, string expected)
        {
            Assert.Equal(expected, input.FirstToLower());
        }
        
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData(" ", " ")]
        [InlineData(" foo", " foo")]
        [InlineData("foo", "Foo")]
        [InlineData("fooBar", "FooBar")]
        public void FirstToUpperTheory(string input, string expected)
        {
            Assert.Equal(expected, input.FirstToUpper());
        }
    }
}
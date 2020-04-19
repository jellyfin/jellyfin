using System;
using MediaBrowser.Common.Extensions;
using Xunit;

namespace Jellyfin.Common.Tests.Extensions
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("Banana split", ' ', "Banana")]
        [InlineData("Banana split", 'q', "Banana split")]
        public void LeftPartCharTest(string str, char needle, string result)
        {
            Assert.Equal(result, str.AsSpan().LeftPart(needle).ToString());
        }

        [Theory]
        [InlineData("Banana split", " ", "Banana")]
        [InlineData("Banana split test", " split", "Banana")]
        public void LeftPartWithoutStringComparisonTest(string str, string needle, string result)
        {
            Assert.Equal(result, str.AsSpan().LeftPart(needle).ToString());
        }

        [Theory]
        [InlineData("Banana split", " ", StringComparison.Ordinal, "Banana")]
        [InlineData("Banana split test", " split", StringComparison.Ordinal, "Banana")]
        [InlineData("Banana split test", " Split", StringComparison.Ordinal, "Banana split test")]
        [InlineData("Banana split test", " Splït", StringComparison.InvariantCultureIgnoreCase, "Banana split test")]
        public void LeftPartTest(string str, string needle, StringComparison stringComparison, string result)
        {
            Assert.Equal(result, str.AsSpan().LeftPart(needle, stringComparison).ToString());
        }
    }
}

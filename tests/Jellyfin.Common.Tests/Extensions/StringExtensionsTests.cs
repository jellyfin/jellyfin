using System;
using MediaBrowser.Common.Extensions;
using Xunit;

namespace Jellyfin.Common.Tests.Extensions
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("", 'q', "")]
        [InlineData("Banana split", ' ', "Banana")]
        [InlineData("Banana split", 'q', "Banana split")]
        public void LeftPart_ValidArgsCharNeedle_Correct(string str, char needle, string expectedResult)
        {
            var result = str.AsSpan().LeftPart(needle).ToString();
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("", "", "")]
        [InlineData("", "q", "")]
        [InlineData("Banana split", "", "")]
        [InlineData("Banana split", " ", "Banana")]
        [InlineData("Banana split test", " split", "Banana")]
        public void LeftPart_ValidArgsWithoutStringComparison_Correct(string str, string needle, string expectedResult)
        {
            var result = str.AsSpan().LeftPart(needle).ToString();
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("", "", StringComparison.Ordinal, "")]
        [InlineData("Banana split", " ", StringComparison.Ordinal, "Banana")]
        [InlineData("Banana split test", " split", StringComparison.Ordinal, "Banana")]
        [InlineData("Banana split test", " Split", StringComparison.Ordinal, "Banana split test")]
        [InlineData("Banana split test", " Splït", StringComparison.InvariantCultureIgnoreCase, "Banana split test")]
        public void LeftPart_ValidArgs_Correct(string str, string needle, StringComparison stringComparison, string expectedResult)
        {
            var result = str.AsSpan().LeftPart(needle, stringComparison).ToString();
            Assert.Equal(expectedResult, result);
        }
    }
}

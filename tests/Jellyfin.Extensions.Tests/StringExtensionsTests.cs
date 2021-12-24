using System;
using Xunit;

namespace Jellyfin.Extensions.Tests
{
    public class StringExtensionsTests
    {
        [Theory]
        [InlineData("", '_', 0)]
        [InlineData("___", '_', 3)]
        [InlineData("test\x00", '\x00', 1)]
        [InlineData("Imdb=tt0119567|Tmdb=330|TmdbCollection=328", '|', 2)]
        public void ReadOnlySpan_Count_Success(string str, char needle, int count)
        {
            Assert.Equal(count, str.AsSpan().Count(needle));
        }

        [Theory]
        [InlineData("", 'q', "")]
        [InlineData("Banana split", ' ', "Banana")]
        [InlineData("Banana split", 'q', "Banana split")]
        [InlineData("Banana split 2", ' ', "Banana")]
        public void LeftPart_ValidArgsCharNeedle_Correct(string str, char needle, string expectedResult)
        {
            var result = str.AsSpan().LeftPart(needle).ToString();
            Assert.Equal(expectedResult, result);
        }

        [Theory]
        [InlineData("", 'q', "")]
        [InlineData("Banana split", ' ', "split")]
        [InlineData("Banana split", 'q', "Banana split")]
        [InlineData("Banana split.", '.', "")]
        [InlineData("Banana split 2", ' ', "2")]
        public void RightPart_ValidArgsCharNeedle_Correct(string str, char needle, string expectedResult)
        {
            var result = str.AsSpan().RightPart(needle).ToString();
            Assert.Equal(expectedResult, result);
        }
    }
}

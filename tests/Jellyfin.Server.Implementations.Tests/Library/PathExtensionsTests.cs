using System;
using Emby.Server.Implementations.Library;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library
{
    public class PathExtensionsTests
    {
        [Theory]
        [InlineData("Superman: Red Son [imdbid=tt10985510]", "imdbid", "tt10985510")]
        [InlineData("Superman: Red Son - tt10985510", "imdbid", "tt10985510")]
        [InlineData("Superman: Red Son", "imdbid", null)]
        public void GetAttributeValue_ValidArgs_Correct(string input, string attribute, string? expectedResult)
        {
            Assert.Equal(expectedResult, PathExtensions.GetAttributeValue(input, attribute));
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("Superman: Red Son [imdbid=tt10985510]", "")]
        [InlineData("", "imdbid")]
        public void GetAttributeValue_EmptyString_ThrowsArgumentException(string input, string attribute)
        {
            Assert.Throws<ArgumentException>(() => PathExtensions.GetAttributeValue(input, attribute));
        }
    }
}

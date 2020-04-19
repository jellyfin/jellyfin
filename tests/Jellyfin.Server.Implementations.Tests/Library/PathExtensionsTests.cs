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
        public void GetAttributeValueTest(string input, string attribute, string? result)
        {
            Assert.Equal(result, PathExtensions.GetAttributeValue(input, attribute));
        }
    }
}

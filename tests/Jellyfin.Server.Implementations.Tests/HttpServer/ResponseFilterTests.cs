using Emby.Server.Implementations.HttpServer;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.HttpServer
{
    public class ResponseFilterTests
    {
        [Theory]
        [InlineData(null, null)]
        [InlineData("", "")]
        [InlineData("This is a clean string.", "This is a clean string.")]
        [InlineData("This isn't \n\ra clean string.", "This isn't a clean string.")]
        public void RemoveControlCharacters_ValidArgs_Correct(string? input, string? result)
        {
            Assert.Equal(result, ResponseFilter.RemoveControlCharacters(input));
        }
    }
}

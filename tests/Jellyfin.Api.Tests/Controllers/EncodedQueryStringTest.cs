using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers
{
    /// <summary>
    /// Defines the test for encoded querystrings in the url.
    /// </summary>
    public class EncodedQueryStringTest : IClassFixture<JellyfinApplicationFactory>
    {
        private readonly JellyfinApplicationFactory _factory;

        public EncodedQueryStringTest(JellyfinApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Ensure_Ping_Working()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("system/ping").ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Theory]
        [InlineData("a=1&b=2&c=3", "a=1&b=2&c=3")] // won't be processed as there is more than 1.
        [InlineData("a=1", "a=1")] // won't be processed as it has a value
        [InlineData("%3D", "==")] // will decode with an empty string value '=' = ''.
        [InlineData("a%3D1%26b%3D2%26c%3D3", "a=1&b=2&c=3")] // will be processed.

        public async Task Ensure_Decoding_Of_Urls_Is_Working(string sourceUrl, string unencodedUrl)
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("system/ping?" + sourceUrl).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(unencodedUrl, response.Headers.GetValues("querystring").First());
        }
    }
}

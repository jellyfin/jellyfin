using System.Globalization;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers
{
    public sealed class MediaInfoControllerTests : IClassFixture<JellyfinApplicationFactory>
    {
        private readonly JellyfinApplicationFactory _factory;
        private static string? _accessToken;

        public MediaInfoControllerTests(JellyfinApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task BitrateTest_Default_Ok()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

            var response = await client.GetAsync("Playback/BitrateTest");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.Octet, response.Content.Headers.ContentType?.MediaType);
            Assert.NotNull(response.Content.Headers.ContentLength);
        }

        [Theory]
        [InlineData(102400)]
        public async Task BitrateTest_WithValidParam_Ok(int size)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

            var response = await client.GetAsync("Playback/BitrateTest?size=" + size.ToString(CultureInfo.InvariantCulture));

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.Octet, response.Content.Headers.ContentType?.MediaType);
            Assert.NotNull(response.Content.Headers.ContentLength);
            Assert.InRange(response.Content.Headers.ContentLength!.Value, size, long.MaxValue);
        }

        [Theory]
        [InlineData(0)] // Zero
        [InlineData(-102400)] // Negative value
        [InlineData(1000000000)] // Too large
        public async Task BitrateTest_InvalidValue_BadRequest(int size)
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

            var response = await client.GetAsync("Playback/BitrateTest?size=" + size.ToString(CultureInfo.InvariantCulture));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}

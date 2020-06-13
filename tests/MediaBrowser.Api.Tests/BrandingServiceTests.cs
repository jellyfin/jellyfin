using System.Text.Json;
using System.Threading.Tasks;
using MediaBrowser.Model.Branding;
using Xunit;

namespace MediaBrowser.Api.Tests
{
    public sealed class BrandingServiceTests : IClassFixture<JellyfinApplicationFactory>
    {
        private readonly JellyfinApplicationFactory _factory;

        public BrandingServiceTests(JellyfinApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetConfiguration_ReturnsCorrectResponse()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Branding/Configuration");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType.ToString());
            var responseBody = await response.Content.ReadAsStreamAsync();
            _ = await JsonSerializer.DeserializeAsync<BrandingOptions>(responseBody);
        }

        [Theory]
        [InlineData("/Branding/Css")]
        [InlineData("/Branding/Css.css")]
        public async Task GetCss_ReturnsCorrectResponse(string url)
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/css", response.Content.Headers.ContentType.ToString());
        }
    }
}

using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using Xunit;

namespace Jellyfin.Server.Integration.Tests
{
    public sealed class OpenApiSpecTests : IClassFixture<JellyfinApplicationFactory>
    {
        private readonly JellyfinApplicationFactory _factory;
        private readonly ITestOutputHelper _outputHelper;

        public OpenApiSpecTests(JellyfinApplicationFactory factory, ITestOutputHelper outputHelper)
        {
            _factory = factory;
            _outputHelper = outputHelper;
        }

        [Fact]
        public async Task GetSpec_ReturnsCorrectResponse()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api-docs/openapi.json", TestContext.Current.CancellationToken);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

            // Write out for publishing
            string outputPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".", "openapi.json"));
            _outputHelper.WriteLine("Writing OpenAPI Spec JSON to '{0}'.", outputPath);
            await using var fs = AsyncFile.Create(outputPath);
            await response.Content.CopyToAsync(fs, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task GetSpec_StopEncodingProcess_PublishedWithoutTheSegmentRoutes()
        {
            var client = _factory.CreateClient();

            using var response = await client.GetAsync("/api-docs/openapi.json", TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();

            using var spec = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
                cancellationToken: TestContext.Current.CancellationToken);
            var paths = spec.RootElement.GetProperty("paths");

            // Clients call this to stop a transcode they started, so SDKs have to be able to generate it.
            var stopEncoding = paths.GetProperty("/Videos/ActiveEncodings").GetProperty("delete");
            Assert.Equal("StopEncodingProcess", stopEncoding.GetProperty("operationId").GetString());

            // The segment routes next to it are only fetched by players following a playlist and stay hidden.
            Assert.False(paths.TryGetProperty("/Videos/{itemId}/hls/{playlistId}/{segmentId}.{segmentContainer}", out _));
            Assert.False(paths.TryGetProperty("/Audio/{itemId}/hls/{segmentId}/stream.mp3", out _));
        }
    }
}

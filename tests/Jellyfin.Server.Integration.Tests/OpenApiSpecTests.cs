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
        public async Task TranscodeTestStart_ResponseSchemaMatchesStatus()
        {
            var client = _factory.CreateClient();
            var response = await client.GetAsync("/api-docs/openapi.json", TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            var endpoint = document.RootElement.GetProperty("paths").GetProperty("/Transcoding/Test");
            var acceptedContent = endpoint.GetProperty("post").GetProperty("responses").GetProperty("202").GetProperty("content");
            var statusContent = endpoint.GetProperty("get").GetProperty("responses").GetProperty("200").GetProperty("content");

            Assert.NotEmpty(acceptedContent.EnumerateObject());
            foreach (var contentType in acceptedContent.EnumerateObject())
            {
                Assert.Equal(
                    "#/components/schemas/TranscodeTestStatusDto",
                    contentType.Value.GetProperty("schema").GetProperty("$ref").GetString());
                Assert.Equal(
                    statusContent.GetProperty(contentType.Name).GetProperty("schema").GetRawText(),
                    contentType.Value.GetProperty("schema").GetRawText());
            }
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
    }
}

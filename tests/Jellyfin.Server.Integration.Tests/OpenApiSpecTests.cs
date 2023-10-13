using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

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
            var response = await client.GetAsync("/api-docs/openapi.json");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

            // Write out for publishing
            string outputPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".", "openapi.json"));
            _outputHelper.WriteLine("Writing OpenAPI Spec JSON to '{0}'.", outputPath);
            await using var fs = File.Create(outputPath);
            await response.Content.CopyToAsync(fs);
        }
    }
}

using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers
{
    public sealed class ActivityLogControllerTests : IClassFixture<JellyfinApplicationFactory>
    {
        private readonly JellyfinApplicationFactory _factory;
        private static string? _accessToken;

        public ActivityLogControllerTests(JellyfinApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ActivityLog_GetEntries_Ok()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

            var response = await client.GetAsync("System/ActivityLog/Entries");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.Json, response.Content.Headers.ContentType?.MediaType);
        }
    }
}

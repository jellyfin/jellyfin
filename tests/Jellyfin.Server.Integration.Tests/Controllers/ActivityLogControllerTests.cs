using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers
{
    [Collection("Controller collection")]
    public sealed class ActivityLogControllerTests
    {
        private readonly JellyfinApplicationFactory _factory;

        public ActivityLogControllerTests(JellyfinApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task ActivityLog_GetEntries_Ok()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(await _factory.GetAccessTokenAsync());

            var response = await client.GetAsync("System/ActivityLog/Entries", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.Json, response.Content.Headers.ContentType?.MediaType);
        }
    }
}

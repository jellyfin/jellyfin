using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Querying;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers
{
    public sealed class ApiKeyControllerTests : IClassFixture<JellyfinApplicationFactory>
    {
        private readonly JellyfinApplicationFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
        private static string? _accessToken;

        public ApiKeyControllerTests(JellyfinApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Post_CreateKey_ReturnsCreatedKey()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

            const string AppName = "ApiKeyControllerTests_App";

            using var response = await client.PostAsync($"/Auth/Keys?app={AppName}", null, TestContext.Current.CancellationToken);

            // The endpoint must return the created key, not an empty 204 No Content.
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var createdKey = await response.Content.ReadFromJsonAsync<AuthenticationInfo>(_jsonOptions, TestContext.Current.CancellationToken);
            Assert.NotNull(createdKey);
            Assert.Equal(AppName, createdKey!.AppName);
            Assert.False(string.IsNullOrWhiteSpace(createdKey.AccessToken));

            // The returned token must match one of the persisted keys.
            using var getResponse = await client.GetAsync("/Auth/Keys", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
            var keys = await getResponse.Content.ReadFromJsonAsync<QueryResult<AuthenticationInfo>>(_jsonOptions, TestContext.Current.CancellationToken);
            Assert.NotNull(keys);
            Assert.Contains(keys!.Items, k => k.AccessToken == createdKey.AccessToken);
        }
    }
}

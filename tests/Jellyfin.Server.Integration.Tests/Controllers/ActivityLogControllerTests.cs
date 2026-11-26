using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Querying;
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

            var response = await client.GetAsync("System/ActivityLog/Entries", TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.Json, response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task ActivityLog_GetEntries_ExcludeType_Ok()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

            var all = await client.GetFromJsonAsync<QueryResult<ActivityLogEntry>>("System/ActivityLog/Entries", JsonDefaults.Options, TestContext.Current.CancellationToken);
            Assert.NotNull(all);
            Assert.NotEmpty(all.Items);

            var type = all.Items[0].Type;
            var excluded = await client.GetFromJsonAsync<QueryResult<ActivityLogEntry>>($"System/ActivityLog/Entries?excludeType={Uri.EscapeDataString(type)}", JsonDefaults.Options, TestContext.Current.CancellationToken);
            Assert.NotNull(excluded);
            Assert.DoesNotContain(excluded.Items, e => e.Type.Contains(type, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(all.Items.Count(e => !e.Type.Contains(type, StringComparison.OrdinalIgnoreCase)), excluded.TotalRecordCount);
        }
    }
}

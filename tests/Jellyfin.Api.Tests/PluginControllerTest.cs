using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MediaBrowser.Common.Json;
using MediaBrowser.Model.Plugins;
using Xunit;

namespace Jellyfin.Api.Tests
{
    public sealed class PluginControllerTest : IClassFixture<JellyfinApplicationFactory>
    {
        private readonly JellyfinApplicationFactory _factory;
        private readonly JsonSerializerOptions _jsonOpions = JsonDefaults.GetOptions();

        public PluginControllerTest(JellyfinApplicationFactory factory)
        {
            _factory = factory;
        }

        public async Task GetPluginTestResult(Guid pluginId)
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync($"/Plugins/{pluginId}/SelfTest").ConfigureAwait(false);

            Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);

            var res = await response.Content.ReadAsStreamAsync();
            var msg = await JsonSerializer.DeserializeAsync<string>(res, _jsonOpions);

            Assert.True(response.StatusCode == HttpStatusCode.OK, msg);
        }

        [Fact]
        public async Task RunPluginSelfTests()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/Plugins").ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);

            var res = await response.Content.ReadAsStreamAsync();
            var plugins = await JsonSerializer.DeserializeAsync<IEnumerable<PluginInfo>>(res, _jsonOpions);
            if (plugins != null)
            {
                foreach (var plugin in plugins)
                {
                    await GetPluginTestResult(plugin.Id);
                }
            }
        }
    }
}

using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Models;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers
{
    public sealed class DashboardControllerTests : IClassFixture<JellyfinApplicationFactory>
    {
        private readonly JellyfinApplicationFactory _factory;

        public DashboardControllerTests(JellyfinApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetDashboardConfigurationPage_NonExistingPage_NotFound()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("web/ConfigurationPage/ThisPageTotally/Doesnt/Exists.html").ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetDashboardConfigurationPage_ExistingPage_CorrectPage()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/web/ConfigurationPage?name=TestPlugin").ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);
            Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
            StreamReader reader = new StreamReader(typeof(TestPlugin).Assembly.GetManifestResourceStream("Jellyfin.Api.Tests.TestPage.html")!);
            Assert.Equal(await response.Content.ReadAsStringAsync(), reader.ReadToEnd());
        }

        [Fact]
        public async Task GetDashboardConfigurationPage_BrokenPage_NotFound()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/web/ConfigurationPage?name=BrokenPage").ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetConfigurationPages_NoParams_AllConfigurationPages()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/web/ConfigurationPages").ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);
            var res = await JsonSerializer.DeserializeAsync<ConfigurationPageInfo[]>(await response.Content.ReadAsStreamAsync());
            // TODO: check content
        }

        [Fact]
        public async Task GetConfigurationPages_True_MainMenuConfigurationPages()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/web/ConfigurationPages?enableInMainMenu=true").ConfigureAwait(false);

            Assert.True(response.IsSuccessStatusCode);
            var res = await JsonSerializer.DeserializeAsync<ConfigurationPageInfo[]>(await response.Content.ReadAsStreamAsync());
            Assert.Empty(res);
        }
    }
}

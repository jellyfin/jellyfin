using System.IO;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Models;
using Jellyfin.Extensions.Json;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers
{
    public sealed class DashboardControllerTests : IClassFixture<JellyfinApplicationFactory>
    {
        private readonly JellyfinApplicationFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
        private static string? _accessToken;

        public DashboardControllerTests(JellyfinApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetDashboardConfigurationPage_NonExistingPage_NotFound()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("web/ConfigurationPage?name=ThisPageDoesntExists");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetDashboardConfigurationPage_ExistingPage_CorrectPage()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/web/ConfigurationPage?name=TestPlugin");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(MediaTypeNames.Text.Html, response.Content.Headers.ContentType?.MediaType);
            StreamReader reader = new StreamReader(typeof(TestPlugin).Assembly.GetManifestResourceStream("Jellyfin.Server.Integration.Tests.TestPage.html")!);
            Assert.Equal(await response.Content.ReadAsStringAsync(), await reader.ReadToEndAsync());
        }

        [Fact]
        public async Task GetDashboardConfigurationPage_BrokenPage_NotFound()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/web/ConfigurationPage?name=BrokenPage");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetConfigurationPages_NoParams_AllConfigurationPages()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

            var response = await client.GetAsync("/web/ConfigurationPages");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            _ = await response.Content.ReadFromJsonAsync<ConfigurationPageInfo[]>(_jsonOptions);
            // TODO: check content
        }

        [Fact]
        public async Task GetConfigurationPages_True_MainMenuConfigurationPages()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

            var response = await client.GetAsync("/web/ConfigurationPages?enableInMainMenu=true");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.Json, response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(Encoding.UTF8.BodyName, response.Content.Headers.ContentType?.CharSet);

            var data = await response.Content.ReadFromJsonAsync<ConfigurationPageInfo[]>(_jsonOptions);
            Assert.NotNull(data);
            Assert.Empty(data);
        }
    }
}

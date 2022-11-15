using System;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Model.Dlna;
using Xunit;
using Xunit.Priority;

namespace Jellyfin.Server.Integration.Tests.Controllers
{
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public sealed class DlnaControllerTests : IClassFixture<JellyfinApplicationFactory>
    {
        private const string NonExistentProfile = "1322f35b8f2c434dad3cc07c9b97dbd1";
        private readonly JellyfinApplicationFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
        private static string? _accessToken;
        private static string? _newDeviceProfileId;

        public DlnaControllerTests(JellyfinApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        [Priority(0)]
        public async Task GetProfile_DoesNotExist_NotFound()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

            using var response = await client.GetAsync("/Dlna/Profiles/" + NonExistentProfile).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [Priority(0)]
        public async Task DeleteProfile_DoesNotExist_NotFound()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

            using var response = await client.DeleteAsync("/Dlna/Profiles/" + NonExistentProfile).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [Priority(0)]
        public async Task UpdateProfile_DoesNotExist_NotFound()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

            var deviceProfile = new DeviceProfile()
            {
                Name = "ThisProfileDoesNotExist"
            };

            using var response = await client.PostAsJsonAsync("/Dlna/Profiles/" + NonExistentProfile, deviceProfile, _jsonOptions).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [Priority(1)]
        public async Task CreateProfile_Valid_NoContent()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

            var deviceProfile = new DeviceProfile()
            {
                Name = "ThisProfileIsNew"
            };

            using var response = await client.PostAsJsonAsync("/Dlna/Profiles", deviceProfile, _jsonOptions).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        [Priority(2)]
        public async Task GetProfileInfos_Valid_ContainsThisProfileIsNew()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

            using var response = await client.GetAsync("/Dlna/ProfileInfos").ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.Json, response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(Encoding.UTF8.BodyName, response.Content.Headers.ContentType?.CharSet);

            var profiles = await JsonSerializer.DeserializeAsync<DeviceProfileInfo[]>(
                await response.Content.ReadAsStreamAsync().ConfigureAwait(false),
                _jsonOptions).ConfigureAwait(false);

            var newProfile = profiles?.FirstOrDefault(x => string.Equals(x.Name, "ThisProfileIsNew", StringComparison.Ordinal));
            Assert.NotNull(newProfile);
            _newDeviceProfileId = newProfile!.Id;
        }

        [Fact]
        [Priority(3)]
        public async Task UpdateProfile_Valid_NoContent()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

            var updatedProfile = new DeviceProfile()
            {
                Name = "ThisProfileIsUpdated",
                Id = _newDeviceProfileId
            };

            using var postResponse = await client.PostAsJsonAsync("/Dlna/Profiles/" + _newDeviceProfileId, updatedProfile, _jsonOptions).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.NoContent, postResponse.StatusCode);

            // Verify that the profile got updated
            using var response = await client.GetAsync("/Dlna/ProfileInfos").ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.Json, response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(Encoding.UTF8.BodyName, response.Content.Headers.ContentType?.CharSet);

            var profiles = await JsonSerializer.DeserializeAsync<DeviceProfileInfo[]>(
                await response.Content.ReadAsStreamAsync().ConfigureAwait(false),
                _jsonOptions).ConfigureAwait(false);

            Assert.Null(profiles?.FirstOrDefault(x => string.Equals(x.Name, "ThisProfileIsNew", StringComparison.Ordinal)));
            var newProfile = profiles?.FirstOrDefault(x => string.Equals(x.Name, "ThisProfileIsUpdated", StringComparison.Ordinal));
            Assert.NotNull(newProfile);
            _newDeviceProfileId = newProfile!.Id;
        }

        [Fact]
        [Priority(5)]
        public async Task DeleteProfile_Valid_NoContent()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

            using var deleteResponse = await client.DeleteAsync("/Dlna/Profiles/" + _newDeviceProfileId).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            // Verify that the profile got deleted
            using var response = await client.GetAsync("/Dlna/ProfileInfos").ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(MediaTypeNames.Application.Json, response.Content.Headers.ContentType?.MediaType);
            Assert.Equal(Encoding.UTF8.BodyName, response.Content.Headers.ContentType?.CharSet);

            var profiles = await JsonSerializer.DeserializeAsync<DeviceProfileInfo[]>(
                await response.Content.ReadAsStreamAsync().ConfigureAwait(false),
                _jsonOptions).ConfigureAwait(false);

            Assert.Null(profiles?.FirstOrDefault(x => string.Equals(x.Name, "ThisProfileIsUpdated", StringComparison.Ordinal)));
        }
    }
}

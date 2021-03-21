using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Models.StartupDtos;
using MediaBrowser.Common.Json;
using Xunit;
using Xunit.Priority;

namespace Jellyfin.Server.Integration.Tests.Controllers
{
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public sealed class StartupControllerTests : IClassFixture<JellyfinApplicationFactory>
    {
        private readonly JellyfinApplicationFactory _factory;
        private readonly JsonSerializerOptions _jsonOpions = JsonDefaults.Options;

        public StartupControllerTests(JellyfinApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        [Priority(0)]
        public async Task Configuration_EditConfig_Success()
        {
            var client = _factory.CreateClient();

            using var res0 = await client.GetAsync("/Startup/Configuration").ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, res0.StatusCode);
            Assert.Equal(MediaTypeNames.Application.Json, res0.Content.Headers.ContentType?.MediaType);

            var content0 = await res0.Content.ReadAsStreamAsync().ConfigureAwait(false);
            _ = await JsonSerializer.DeserializeAsync<StartupConfigurationDto>(content0, _jsonOpions).ConfigureAwait(false);

            var newConfig = new StartupConfigurationDto()
            {
                UICulture = "NewCulture",
                MetadataCountryCode = "be",
                PreferredMetadataLanguage = "nl"
            };

            var req1 = JsonSerializer.SerializeToUtf8Bytes(newConfig, _jsonOpions);
            using var reqContent1 = new ByteArrayContent(req1);
            reqContent1.Headers.ContentType = MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json);
            var res1 = await client.PostAsync("/Startup/Configuration", reqContent1).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.NoContent, res1.StatusCode);

            var res2 = await client.GetAsync("/Startup/Configuration").ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
            Assert.Equal(MediaTypeNames.Application.Json, res2.Content.Headers.ContentType?.MediaType);

            var content2 = await res2.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var config2 = await JsonSerializer.DeserializeAsync<StartupConfigurationDto>(content2, _jsonOpions).ConfigureAwait(false);
            Assert.Equal(newConfig.UICulture, config2!.UICulture);
            Assert.Equal(newConfig.MetadataCountryCode, config2.MetadataCountryCode);
            Assert.Equal(newConfig.PreferredMetadataLanguage, config2.PreferredMetadataLanguage);
        }

        [Fact]
        [Priority(0)]
        public async Task User_EditUser_Success()
        {
            var client = _factory.CreateClient();

            using var res0 = await client.GetAsync("/Startup/User").ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, res0.StatusCode);
            Assert.Equal(MediaTypeNames.Application.Json, res0.Content.Headers.ContentType?.MediaType);

            var content0 = await res0.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var user = await JsonSerializer.DeserializeAsync<StartupUserDto>(content0, _jsonOpions).ConfigureAwait(false);

            user!.Name = "NewName";
            user.Password = "NewPassword";

            var req1 = JsonSerializer.SerializeToUtf8Bytes(user, _jsonOpions);
            using var reqContent1 = new ByteArrayContent(req1);
            reqContent1.Headers.ContentType = MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json);
            var res1 = await client.PostAsync("/Startup/User", reqContent1).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.NoContent, res1.StatusCode);

            var res2 = await client.GetAsync("/Startup/User").ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, res2.StatusCode);
            Assert.Equal(MediaTypeNames.Application.Json, res2.Content.Headers.ContentType?.MediaType);

            var content2 = await res2.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var user2 = await JsonSerializer.DeserializeAsync<StartupUserDto>(content2, _jsonOpions).ConfigureAwait(false);
            Assert.Equal(user.Name, user2!.Name);
            Assert.NotEmpty(user2.Password);
            Assert.NotEqual(user.Password, user2.Password);
        }

        [Fact]
        [Priority(1)]
        public async Task CompleteWizard_Success()
        {
            var client = _factory.CreateClient();

            var res = await client.PostAsync("/Startup/Complete", new ByteArrayContent(Array.Empty<byte>())).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        [Fact]
        [Priority(2)]
        public async Task GetFirstUser_CompleteWizard_Unauthorized()
        {
            var client = _factory.CreateClient();

            using var res0 = await client.GetAsync("/Startup/User").ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.Unauthorized, res0.StatusCode);
        }
    }
}

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Models.LibraryStructureDto;
using Jellyfin.Extensions.Json;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers
{
    public sealed class MediaStructureControllerTests : IClassFixture<JellyfinApplicationFactory>
    {
        private readonly JellyfinApplicationFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
        private static string? _accessToken;

        public MediaStructureControllerTests(JellyfinApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task RenameVirtualFolder_WhiteSpaceName_ReturnsBadRequest()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

            using var postContent = new ByteArrayContent(Array.Empty<byte>());
            var response = await client.PostAsync("Library/VirtualFolders/Name?name=+&newName=test", postContent).ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RenameVirtualFolder_WhiteSpaceNewName_ReturnsBadRequest()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

            using var postContent = new ByteArrayContent(Array.Empty<byte>());
            var response = await client.PostAsync("Library/VirtualFolders/Name?name=test&newName=+", postContent).ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RenameVirtualFolder_NameDoesntExist_ReturnsNotFound()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

            using var postContent = new ByteArrayContent(Array.Empty<byte>());
            var response = await client.PostAsync("Library/VirtualFolders/Name?name=doesnt+exist&newName=test", postContent).ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task AddMediaPath_PathDoesntExist_ReturnsNotFound()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

            var data = new MediaPathDto()
            {
                Name = "Test",
                Path = "/this/path/doesnt/exist"
            };

            using var postContent = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(data, _jsonOptions));
            postContent.Headers.ContentType = MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json);
            var response = await client.PostAsync("Library/VirtualFolders/Paths", postContent).ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateMediaPath_EmptyName_ReturnsBadRequest()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

            var data = new UpdateMediaPathRequestDto()
            {
                Name = string.Empty,
            };

            using var postContent = new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(data, _jsonOptions));
            postContent.Headers.ContentType = MediaTypeHeaderValue.Parse(MediaTypeNames.Application.Json);
            var response = await client.PostAsync("Library/VirtualFolders/Paths/Update", postContent).ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RemoveMediaPath_WhiteSpaceName_ReturnsBadRequest()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

            var response = await client.DeleteAsync("Library/VirtualFolders/Paths?name=+").ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task RemoveMediaPath_PathDoesntExist_ReturnsNotFound()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

            var response = await client.DeleteAsync("Library/VirtualFolders/Paths?name=none&path=%2Fthis%2Fpath%2Fdoesnt%2Fexist").ConfigureAwait(false);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}

using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Models.StartupDtos;
using Jellyfin.Api.Models.UserDtos;
using Jellyfin.Extensions.Json;
using MediaBrowser.Model.Dto;
using Xunit;

namespace Jellyfin.Server.Integration.Tests
{
    public static class AuthHelper
    {
        public const string AuthHeaderName = "X-Emby-Authorization";
        public const string DummyAuthHeader = "MediaBrowser Client=\"Jellyfin.Server Integration Tests\", DeviceId=\"69420\", Device=\"Apple II\", Version=\"10.8.0\"";

        public static async Task<string> CompleteStartupAsync(HttpClient client)
        {
            var jsonOptions = JsonDefaults.Options;
            var userResponse = await client.GetByteArrayAsync("/Startup/User").ConfigureAwait(false);
            var user = JsonSerializer.Deserialize<StartupUserDto>(userResponse, jsonOptions);

            using var completeResponse = await client.PostAsync("/Startup/Complete", new ByteArrayContent(Array.Empty<byte>())).ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.NoContent, completeResponse.StatusCode);

            using var content = JsonContent.Create(
                new AuthenticateUserByName()
                {
                    Username = user!.Name,
                    Pw = user.Password,
                },
                options: jsonOptions);
            content.Headers.Add("X-Emby-Authorization", DummyAuthHeader);

            using var authResponse = await client.PostAsync("/Users/AuthenticateByName", content).ConfigureAwait(false);
            var auth = await JsonSerializer.DeserializeAsync<AuthenticationResultDto>(
                await authResponse.Content.ReadAsStreamAsync().ConfigureAwait(false),
                jsonOptions).ConfigureAwait(false);

            return auth!.AccessToken;
        }

        public static async Task<UserDto> GetUserDtoAsync(HttpClient client)
        {
            using var response = await client.GetAsync("Users/Me").ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var userDto = await JsonSerializer.DeserializeAsync<UserDto>(
                    await response.Content.ReadAsStreamAsync().ConfigureAwait(false), JsonDefaults.Options).ConfigureAwait(false);
            Assert.NotNull(userDto);
            return userDto;
        }

        public static async Task<BaseItemDto> GetRootFolderDtoAsync(HttpClient client, Guid userId = default)
        {
            if (userId.Equals(default))
            {
                var userDto = await GetUserDtoAsync(client).ConfigureAwait(false);
                userId = userDto.Id;
            }

            var response = await client.GetAsync($"Users/{userId}/Items/Root").ConfigureAwait(false);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var rootDto = await JsonSerializer.DeserializeAsync<BaseItemDto>(
                    await response.Content.ReadAsStreamAsync().ConfigureAwait(false),
                    JsonDefaults.Options).ConfigureAwait(false);
            Assert.NotNull(rootDto);
            return rootDto;
        }

        public static void AddAuthHeader(this HttpHeaders headers, string accessToken)
        {
            headers.Add(AuthHeaderName, DummyAuthHeader + $", Token={accessToken}");
        }

        private sealed class AuthenticationResultDto
        {
            public string AccessToken { get; set; } = string.Empty;

            public string ServerId { get; set; } = string.Empty;
        }
    }
}

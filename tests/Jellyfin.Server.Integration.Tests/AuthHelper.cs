using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Models.StartupDtos;
using Jellyfin.Api.Models.UserDtos;
using Jellyfin.Extensions;
using Jellyfin.Extensions.Json;
using MediaBrowser.Model.Dto;
using Xunit;

namespace Jellyfin.Server.Integration.Tests
{
    public static class AuthHelper
    {
        public const string AuthHeaderName = "Authorization";
        public const string DummyAuthHeader = "MediaBrowser Client=\"Jellyfin.Server%20Integration%20Tests\", DeviceId=\"69420\", Device=\"Apple%20II\", Version=\"10.8.0\"";

        public static async Task<string> CompleteStartupAsync(HttpClient client)
        {
            var jsonOptions = JsonDefaults.Options;
            var userResponse = await client.GetByteArrayAsync("/Startup/User");
            var user = JsonSerializer.Deserialize<StartupUserDto>(userResponse, jsonOptions);

            using var completeResponse = await client.PostAsync("/Startup/Complete", new ByteArrayContent(Array.Empty<byte>()));
            Assert.Equal(HttpStatusCode.NoContent, completeResponse.StatusCode);

            using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "/Users/AuthenticateByName");
            httpRequest.Headers.TryAddWithoutValidation(AuthHeaderName, DummyAuthHeader);
            httpRequest.Content = JsonContent.Create(
                new AuthenticateUserByName()
                {
                    Username = user!.Name,
                    Pw = user.Password,
                },
                options: jsonOptions);

            using var authResponse = await client.SendAsync(httpRequest);
            authResponse.EnsureSuccessStatusCode();

            var auth = await authResponse.Content.ReadFromJsonAsync<AuthenticationResultDto>(jsonOptions);

            return auth!.AccessToken;
        }

        public static async Task<UserDto> GetUserDtoAsync(HttpClient client)
        {
            using var response = await client.GetAsync("Users/Me");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var userDto = await response.Content.ReadFromJsonAsync<UserDto>(JsonDefaults.Options);
            Assert.NotNull(userDto);
            return userDto;
        }

        public static async Task<BaseItemDto> GetRootFolderDtoAsync(HttpClient client, Guid userId = default)
        {
            if (userId.IsEmpty())
            {
                var userDto = await GetUserDtoAsync(client);
                userId = userDto.Id;
            }

            var response = await client.GetAsync($"Users/{userId}/Items/Root");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var rootDto = await response.Content.ReadFromJsonAsync<BaseItemDto>(JsonDefaults.Options);
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

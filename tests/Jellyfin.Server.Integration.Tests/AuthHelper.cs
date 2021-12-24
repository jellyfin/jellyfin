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

        public static void AddAuthHeader(this HttpHeaders headers, string accessToken)
        {
            headers.Add(AuthHeaderName, DummyAuthHeader + $", Token={accessToken}");
        }

        private class AuthenticationResultDto
        {
            public string AccessToken { get; set; } = string.Empty;

            public string ServerId { get; set; } = string.Empty;
        }
    }
}

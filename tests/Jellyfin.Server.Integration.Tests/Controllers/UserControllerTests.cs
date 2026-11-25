using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Models.UserDtos;
using Jellyfin.Extensions.Json;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Users;
using Xunit;
using Xunit.v3.Priority;

namespace Jellyfin.Server.Integration.Tests.Controllers
{
    [TestCaseOrderer(typeof(PriorityOrderer))]
    public sealed class UserControllerTests : IClassFixture<JellyfinApplicationFactory>
    {
        private const string TestUsername = "testUser01";

        private readonly JellyfinApplicationFactory _factory;
        private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
        private static string? _accessToken;
        private static Guid _testUserId = Guid.Empty;

        public UserControllerTests(JellyfinApplicationFactory factory)
        {
            _factory = factory;
        }

        private Task<HttpResponseMessage> CreateUserByName(HttpClient httpClient, CreateUserByName request)
            => httpClient.PostAsJsonAsync("Users/New", request, _jsonOptions);

        private Task<HttpResponseMessage> UpdateUserPassword(HttpClient httpClient, Guid userId, UpdateUserPassword request)
            => httpClient.PostAsJsonAsync("Users/" + userId.ToString("N", CultureInfo.InvariantCulture) + "/Password", request, _jsonOptions);

        [Fact]
        [Priority(-1)]
        public async Task GetPublicUsers_Valid_Success()
        {
            var client = _factory.CreateClient();

            using var response = await client.GetAsync("Users/Public", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var users = await response.Content.ReadFromJsonAsync<UserDto[]>(_jsonOptions, TestContext.Current.CancellationToken);
            // User are hidden by default
            Assert.NotNull(users);
            Assert.Empty(users);
        }

        [Fact]
        [Priority(-1)]
        public async Task GetUsers_Valid_Success()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

            using var response = await client.GetAsync("Users", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var users = await response.Content.ReadFromJsonAsync<UserDto[]>(_jsonOptions, TestContext.Current.CancellationToken);
            Assert.NotNull(users);
            Assert.Single(users);
        }

        [Fact]
        [Priority(-1)]
        public async Task Me_Valid_Success()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

            _ = await AuthHelper.GetUserDtoAsync(client);
        }

        [Fact]
        [Priority(0)]
        public async Task New_Valid_Success()
        {
            var client = _factory.CreateClient();

            // access token can't be null here as the previous test populated it
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken!);

            var createRequest = new CreateUserByName()
            {
                Name = TestUsername
            };

            using var response = await CreateUserByName(client, createRequest);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var user = await response.Content.ReadFromJsonAsync<UserDto>(_jsonOptions, TestContext.Current.CancellationToken);
            Assert.Equal(TestUsername, user!.Name);

            _testUserId = user.Id;

            Console.WriteLine(user.Id.ToString("N", CultureInfo.InvariantCulture));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("‼️")]
        [Priority(0)]
        public async Task New_Invalid_Fail(string? username)
        {
            var client = _factory.CreateClient();

            // access token can't be null here as the previous test populated it
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken!);

            var createRequest = new CreateUserByName()
            {
                Name = username!
            };

            using var response = await CreateUserByName(client, createRequest);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        [Priority(0)]
        public async Task Delete_DoesntExist_NotFound()
        {
            var client = _factory.CreateClient();

            // access token can't be null here as the previous test populated it
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken!);

            using var response = await client.DeleteAsync($"User/{Guid.NewGuid()}", TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        [Priority(1)]
        public async Task UpdateUserPassword_Valid_Success()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken!);

            var createRequest = new UpdateUserPassword()
            {
                NewPw = "4randomPa$$word"
            };

            using var response = await UpdateUserPassword(client, _testUserId, createRequest);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        [Priority(2)]
        public async Task UpdateUserPassword_Empty_RemoveSetPassword()
        {
            var client = _factory.CreateClient();

            client.DefaultRequestHeaders.AddAuthHeader(_accessToken!);

            var createRequest = new UpdateUserPassword()
            {
                CurrentPw = "4randomPa$$word",
            };

            using var response = await UpdateUserPassword(client, _testUserId, createRequest);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        [Priority(2)]
        public async Task UpdateUser_UsernameCaseDifference_Success()
        {
            var client = _factory.CreateClient();

            client.DefaultRequestHeaders.AddAuthHeader(_accessToken!);

            using var response = await client.GetAsync("Users/" + _testUserId, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var userDto = await response.Content.ReadFromJsonAsync<UserDto>(JsonDefaults.Options, TestContext.Current.CancellationToken);
            Assert.NotNull(userDto);

            userDto.Name = userDto.Name.ToLowerInvariant();

            using var response2 = await client.PostAsJsonAsync($"Users?userId={_testUserId}", userDto, _jsonOptions, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NoContent, response2.StatusCode);

            using var response3 = await client.GetAsync("Users/" + _testUserId, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var newUserDto = await response3.Content.ReadFromJsonAsync<UserDto>(JsonDefaults.Options, TestContext.Current.CancellationToken);
            Assert.NotNull(newUserDto);
            Assert.Equal(userDto.Name, newUserDto.Name);

            // Sanity check, make sure we're testing something
            Assert.NotEqual(TestUsername, userDto.Name);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(int.MaxValue)]
        [Priority(1)]
        public async Task UpdateUserPolicy_ValidVideoLimit_Persists(int limit)
        {
            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));
            var user = await CreateVideoPolicyTestUserAsync(client);
            try
            {
                user.Policy!.MaxActiveVideoStreams = limit;
                using var response = await client.PostAsJsonAsync($"Users/{user.Id}/Policy", user.Policy, _jsonOptions, TestContext.Current.CancellationToken);

                Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
                Assert.Equal(limit, (await GetPolicyAsync(client, user.Id)).MaxActiveVideoStreams);
            }
            finally
            {
                using var response = await client.DeleteAsync($"Users/{user.Id}", TestContext.Current.CancellationToken);
                response.EnsureSuccessStatusCode();
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [Priority(1)]
        public async Task UpdateUserPolicy_NegativeVideoLimit_ReturnsBadRequestWithoutChangingPolicy(bool camelCase)
        {
            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));
            var user = await CreateVideoPolicyTestUserAsync(client);
            try
            {
                user.Policy!.MaxActiveVideoStreams = 2;
                using var seedResponse = await client.PostAsJsonAsync($"Users/{user.Id}/Policy", user.Policy, _jsonOptions, TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.NoContent, seedResponse.StatusCode);

                user.Policy.MaxActiveVideoStreams = -1;
                user.Policy.MaxActiveSessions = 7;
                var options = camelCase ? JsonDefaults.CamelCaseOptions : JsonDefaults.PascalCaseOptions;
                using var response = await client.PostAsJsonAsync($"Users/{user.Id}/Policy", user.Policy, options, TestContext.Current.CancellationToken);

                Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
                Assert.Contains(nameof(UserPolicy.MaxActiveVideoStreams), await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken), StringComparison.OrdinalIgnoreCase);
                var stored = await GetPolicyAsync(client, user.Id);
                Assert.Equal(2, stored.MaxActiveVideoStreams);
                Assert.Equal(0, stored.MaxActiveSessions);
            }
            finally
            {
                using var response = await client.DeleteAsync($"Users/{user.Id}", TestContext.Current.CancellationToken);
                response.EnsureSuccessStatusCode();
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        [Priority(1)]
        public async Task UpdateUserPolicy_OmittedVideoLimitPreservesLimitAndExplicitZeroDisablesIt(bool camelCase)
        {
            using var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));
            var user = await CreateVideoPolicyTestUserAsync(client);
            try
            {
                user.Policy!.MaxActiveVideoStreams = 2;
                using var seedResponse = await client.PostAsJsonAsync($"Users/{user.Id}/Policy", user.Policy, _jsonOptions, TestContext.Current.CancellationToken);
                Assert.Equal(HttpStatusCode.NoContent, seedResponse.StatusCode);

                var options = camelCase ? JsonDefaults.CamelCaseOptions : JsonDefaults.PascalCaseOptions;
                var propertyName = camelCase ? "maxActiveVideoStreams" : "MaxActiveVideoStreams";
                user.Policy.MaxActiveSessions = 3;
                var oldClientPolicy = JsonSerializer.SerializeToNode(user.Policy, options)!.AsObject();
                Assert.True(oldClientPolicy.Remove(propertyName));
                using var omittedResponse = await client.PostAsJsonAsync($"Users/{user.Id}/Policy", oldClientPolicy, options, TestContext.Current.CancellationToken);

                Assert.Equal(HttpStatusCode.NoContent, omittedResponse.StatusCode);
                var stored = await GetPolicyAsync(client, user.Id);
                Assert.Equal(2, stored.MaxActiveVideoStreams);
                Assert.Equal(3, stored.MaxActiveSessions);

                oldClientPolicy[propertyName] = 0;
                using var zeroResponse = await client.PostAsJsonAsync($"Users/{user.Id}/Policy", oldClientPolicy, options, TestContext.Current.CancellationToken);

                Assert.Equal(HttpStatusCode.NoContent, zeroResponse.StatusCode);
                Assert.Equal(0, (await GetPolicyAsync(client, user.Id)).MaxActiveVideoStreams);
            }
            finally
            {
                using var response = await client.DeleteAsync($"Users/{user.Id}", TestContext.Current.CancellationToken);
                response.EnsureSuccessStatusCode();
            }
        }

        [Fact]
        public async Task UserPolicySchema_ExposesOnlyTheInt32VideoLimit()
        {
            using var client = _factory.CreateClient();
            using var response = await client.GetAsync("api-docs/openapi.json", TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
            var properties = document.RootElement.GetProperty("components").GetProperty("schemas").GetProperty("UserPolicy").GetProperty("properties");
            var limit = properties.GetProperty(nameof(UserPolicy.MaxActiveVideoStreams));

            Assert.Equal("integer", limit.GetProperty("type").GetString());
            Assert.Equal("int32", limit.GetProperty("format").GetString());
            Assert.Equal(0, limit.GetProperty("minimum").GetInt32());
            Assert.False(properties.TryGetProperty(nameof(UserPolicy.HasMaxActiveVideoStreams), out _));
        }

        private async Task<UserDto> CreateVideoPolicyTestUserAsync(HttpClient client)
        {
            using var response = await CreateUserByName(client, new CreateUserByName { Name = $"video-policy-{Guid.NewGuid():N}" });
            response.EnsureSuccessStatusCode();
            var user = await response.Content.ReadFromJsonAsync<UserDto>(_jsonOptions, TestContext.Current.CancellationToken);
            Assert.NotNull(user);
            return user;
        }

        private async Task<UserPolicy> GetPolicyAsync(HttpClient client, Guid userId)
        {
            using var response = await client.GetAsync($"Users/{userId}", TestContext.Current.CancellationToken);
            response.EnsureSuccessStatusCode();
            var user = await response.Content.ReadFromJsonAsync<UserDto>(_jsonOptions, TestContext.Current.CancellationToken);
            Assert.NotNull(user?.Policy);
            return user.Policy;
        }
    }
}

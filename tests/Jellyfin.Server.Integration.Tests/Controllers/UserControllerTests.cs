using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Models.UserDtos;
using Jellyfin.Extensions.Json;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Users; // Added for UserPolicy
using Xunit;
using Xunit.Priority;

namespace Jellyfin.Server.Integration.Tests.Controllers
{
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
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

            using var response = await client.GetAsync("Users/Public");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var users = await response.Content.ReadFromJsonAsync<UserDto[]>(_jsonOptions);
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

            using var response = await client.GetAsync("Users");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var users = await response.Content.ReadFromJsonAsync<UserDto[]>(_jsonOptions);
            Assert.NotNull(users);
            Assert.Single(users);
            Assert.False(users![0].HasConfiguredPassword);
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

            var testEmail = "testuser@example.com";
            var createRequest = new CreateUserByName()
            {
                Name = TestUsername,
                Email = testEmail
            };

            using var response = await CreateUserByName(client, createRequest);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var user = await response.Content.ReadFromJsonAsync<UserDto>(_jsonOptions);
            Assert.Equal(TestUsername, user!.Name);
            Assert.Equal(testEmail, user.Email);
            Assert.False(user.HasPassword);
            Assert.False(user.HasConfiguredPassword);

            _testUserId = user.Id;

            Console.WriteLine(user.Id.ToString("N", CultureInfo.InvariantCulture));
        }

        [Fact]
        [Priority(0)] // Run after user creation
        public async Task Update_Email_Valid_Success()
        {
            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken!);

            // First, get the user to ensure we have the latest version for update
            var initialUserResponse = await client.GetAsync($"Users/{_testUserId}");
            Assert.Equal(HttpStatusCode.OK, initialUserResponse.StatusCode);
            var userToUpdate = await initialUserResponse.Content.ReadFromJsonAsync<UserDto>(_jsonOptions);
            Assert.NotNull(userToUpdate);

            var newEmail = "updateduser@example.com";
            userToUpdate.Email = newEmail;

            // The UserController's POST /Users?userId={userId} endpoint is used for updates.
            // Ensure that `updateUser.Configuration` and `updateUser.Policy` are not null if they are required by the DTO.
            // If UserDto has non-nullable Configuration/Policy, and they are null here, this would fail.
            // Let's assume they are initialized by default or correctly fetched.
            if (userToUpdate.Configuration == null)
            {
                userToUpdate.Configuration = new MediaBrowser.Model.Configuration.UserConfiguration();
            }

            if (userToUpdate.Policy == null)
            {
                userToUpdate.Policy = new UserPolicy();
            }

            using var updateResponse = await client.PostAsJsonAsync($"Users/{_testUserId}", userToUpdate, _jsonOptions);
            Assert.Equal(HttpStatusCode.NoContent, updateResponse.StatusCode);

            // Fetch the user again to verify the email was updated
            var updatedUserResponse = await client.GetAsync($"Users/{_testUserId}");
            Assert.Equal(HttpStatusCode.OK, updatedUserResponse.StatusCode);
            var updatedUser = await updatedUserResponse.Content.ReadFromJsonAsync<UserDto>(_jsonOptions);
            Assert.NotNull(updatedUser);
            Assert.Equal(newEmail, updatedUser.Email);
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

            using var response = await client.DeleteAsync($"User/{Guid.NewGuid()}");
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

            var users = await JsonSerializer.DeserializeAsync<UserDto[]>(
                await client.GetStreamAsync("Users"), _jsonOptions);
            var user = users!.First(x => x.Id.Equals(_testUserId));
            Assert.True(user.HasPassword);
            Assert.True(user.HasConfiguredPassword);
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

            var users = await JsonSerializer.DeserializeAsync<UserDto[]>(
                await client.GetStreamAsync("Users"), _jsonOptions);
            var user = users!.First(x => x.Id.Equals(_testUserId));
            Assert.False(user.HasPassword);
            Assert.False(user.HasConfiguredPassword);
        }
    }
}

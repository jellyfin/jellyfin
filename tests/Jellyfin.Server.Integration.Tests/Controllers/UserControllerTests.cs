using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Models.UserDtos;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
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

        private Task<HttpResponseMessage> UpdateUserPolicy(HttpClient httpClient, Guid userId, UserPolicy policy)
            => httpClient.PostAsJsonAsync("Users/" + userId.ToString("N", CultureInfo.InvariantCulture) + "/Policy", policy, _jsonOptions);

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

        [Fact]
        [Priority(3)]
        public async Task AuthenticateUserByName_OutsideAccessSchedule_ReturnsParentalControlErrorCode()
        {
            var adminClient = _factory.CreateClient();
            adminClient.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(adminClient));

            const string Username = "parentalScheduleUser";
            const string Password = "d0ntL3tM31n";

            using var createResponse = await CreateUserByName(
                adminClient,
                new CreateUserByName { Name = Username, Password = Password });
            Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

            var user = await createResponse.Content.ReadFromJsonAsync<UserDto>(_jsonOptions, TestContext.Current.CancellationToken);
            Assert.NotNull(user);

            // A schedule that only permits access tomorrow leaves the user blocked for the whole of today.
            var tomorrow = (DynamicDayOfWeek)(((int)DateTime.Now.DayOfWeek + 1) % 7);
            user.Policy.AccessSchedules = new[] { new AccessSchedule(tomorrow, 0, 24, user.Id) };

            using var policyResponse = await UpdateUserPolicy(adminClient, user.Id, user.Policy);
            Assert.Equal(HttpStatusCode.NoContent, policyResponse.StatusCode);

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.TryAddWithoutValidation(AuthHelper.AuthHeaderName, AuthHelper.DummyAuthHeader);

            using var authResponse = await client.PostAsJsonAsync(
                "Users/AuthenticateByName",
                new AuthenticateUserByName { Username = Username, Pw = Password },
                _jsonOptions,
                TestContext.Current.CancellationToken);

            Assert.Equal(HttpStatusCode.Forbidden, authResponse.StatusCode);
            Assert.Equal("ParentalControl", Assert.Single(authResponse.Headers.GetValues("X-Application-Error-Code")));
        }
    }
}

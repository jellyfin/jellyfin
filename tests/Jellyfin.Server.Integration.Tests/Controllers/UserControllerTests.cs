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
    }
}

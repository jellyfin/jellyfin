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
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Model.Configuration;
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
        private static string? _publicAccessToken;
        private static Guid _publicUserId = Guid.Empty;
        private static Guid _testUserId = Guid.Empty;

        public UserControllerTests(JellyfinApplicationFactory factory)
        {
            _factory = factory;
        }

        private Task<HttpResponseMessage> CreateUserByName(HttpClient httpClient, CreateUserByName request)
            => httpClient.PostAsJsonAsync("Users/New", request, _jsonOptions);

        private Task<HttpResponseMessage> RegisterUser(HttpClient httpClient, CreateUserByName request)
            => httpClient.PostAsJsonAsync("Users/Register", request, _jsonOptions);

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
        [Priority(3)]
        public async Task Register_DefaultDisabled_Forbidden()
        {
            var client = _factory.CreateClient();

            using var response = await RegisterUser(client, new CreateUserByName
            {
                Name = "disabledRegistrationUser",
                Password = "4randomPa$$word"
            });

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        [Priority(4)]
        public async Task Register_RateLimited_TooManyRequests()
        {
            var client = _factory.CreateClient();
            await EnablePublicRegistration(client, maxAttempts: 1);

            using var firstResponse = await RegisterUser(client, new CreateUserByName
            {
                Name = "rateLimitedRegistrationUser1",
                Password = "4randomPa$$word"
            });
            Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

            using var secondResponse = await RegisterUser(client, new CreateUserByName
            {
                Name = "rateLimitedRegistrationUser2",
                Password = "4randomPa$$word"
            });
            Assert.Equal((HttpStatusCode)429, secondResponse.StatusCode);
        }

        [Fact]
        [Priority(5)]
        public async Task Register_Enabled_Success()
        {
            var client = _factory.CreateClient();
            await EnablePublicRegistration(client, maxAttempts: 5);

            using var response = await RegisterUser(client, new CreateUserByName
            {
                Name = "publicRegistrationUser",
                Password = "4randomPa$$word"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var auth = await response.Content.ReadFromJsonAsync<AuthenticationResult>(_jsonOptions, TestContext.Current.CancellationToken);
            Assert.NotNull(auth);
            Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
            Assert.NotNull(auth.User);
            _publicAccessToken = auth.AccessToken;
            _publicUserId = auth.User.Id;
            Assert.Equal("publicRegistrationUser", auth.User.Name);
            Assert.False(auth.User.Policy.IsAdministrator);
            Assert.False(auth.User.Policy.EnableContentDownloading);
            Assert.False(auth.User.Policy.EnableMediaConversion);
            Assert.False(auth.User.Policy.EnableSyncTranscoding);
            Assert.False(auth.User.Policy.EnablePublicSharing);
            Assert.False(auth.User.Policy.EnableSharedDeviceControl);
            Assert.False(auth.User.Policy.EnableAllFolders);
            Assert.Equal(2, auth.User.Policy.MaxActiveSessions);
            Assert.Equal(8_000_000, auth.User.Policy.RemoteClientBitrateLimit);
        }

        [Fact]
        [Priority(6)]
        public async Task PublicAccount_WeakPasswordChangesAreRejectedForUserAndAdministrator()
        {
            var publicClient = _factory.CreateClient();
            publicClient.DefaultRequestHeaders.AddAuthHeader(_publicAccessToken!);

            using var selfShortResponse = await UpdateUserPassword(
                publicClient,
                _publicUserId,
                new UpdateUserPassword
                {
                    CurrentPw = "4randomPa$$word",
                    NewPw = "short"
                });
            Assert.Equal(HttpStatusCode.BadRequest, selfShortResponse.StatusCode);

            using var selfResetResponse = await UpdateUserPassword(
                publicClient,
                _publicUserId,
                new UpdateUserPassword
                {
                    CurrentPw = "4randomPa$$word",
                    ResetPassword = true
                });
            Assert.Equal(HttpStatusCode.BadRequest, selfResetResponse.StatusCode);

            var administratorClient = _factory.CreateClient();
            administratorClient.DefaultRequestHeaders.AddAuthHeader(_accessToken!);

            using var administratorShortResponse = await UpdateUserPassword(
                administratorClient,
                _publicUserId,
                new UpdateUserPassword
                {
                    NewPw = "short"
                });
            Assert.Equal(HttpStatusCode.BadRequest, administratorShortResponse.StatusCode);

            using var administratorResetResponse = await UpdateUserPassword(
                administratorClient,
                _publicUserId,
                new UpdateUserPassword
                {
                    ResetPassword = true
                });
            Assert.Equal(HttpStatusCode.BadRequest, administratorResetResponse.StatusCode);
        }

        [Fact]
        [Priority(6)]
        public async Task Register_Duplicate_Conflict()
        {
            var client = _factory.CreateClient();
            await EnablePublicRegistration(client, maxAttempts: 5);

            using var response = await RegisterUser(client, new CreateUserByName
            {
                Name = "publicRegistrationUser",
                Password = "4randomPa$$word"
            });

            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        [Priority(7)]
        public async Task Register_ShortPassword_BadRequest()
        {
            var client = _factory.CreateClient();
            await EnablePublicRegistration(client, maxAttempts: 5);

            using var response = await RegisterUser(client, new CreateUserByName
            {
                Name = "shortPasswordRegistrationUser",
                Password = "short"
            });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        [Priority(8)]
        public async Task Register_MissingClientMetadata_DoesNotCreateUser()
        {
            var client = _factory.CreateClient();
            await EnablePublicRegistration(client, maxAttempts: 2);
            client.DefaultRequestHeaders.Remove(AuthHelper.AuthHeaderName);

            var request = new CreateUserByName
            {
                Name = "missingMetadataRegistrationUser",
                Password = "4randomPa$$word"
            };
            using var invalidResponse = await RegisterUser(client, request);
            Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);

            client.DefaultRequestHeaders.TryAddWithoutValidation(AuthHelper.AuthHeaderName, AuthHelper.DummyAuthHeader);
            using var validResponse = await RegisterUser(client, request);
            Assert.Equal(HttpStatusCode.OK, validResponse.StatusCode);
        }

        private Task EnablePublicRegistration(HttpClient client, int maxAttempts)
            => ConfigurePublicRegistration(client, enabled: true, maxAttempts);

        private async Task ConfigurePublicRegistration(HttpClient client, bool enabled, int maxAttempts)
        {
            client.DefaultRequestHeaders.Remove(AuthHelper.AuthHeaderName);
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

            var configuration = await client.GetFromJsonAsync<ServerConfiguration>("System/Configuration", _jsonOptions, TestContext.Current.CancellationToken);
            Assert.NotNull(configuration);
            configuration.EnablePublicUserRegistration = enabled;
            configuration.PublicUserRegistrationMaxAttemptsPerWindow = maxAttempts;
            configuration.PublicUserRegistrationWindowSeconds = 600;
            configuration.PublicUserRegistrationMinimumPasswordLength = 8;
            configuration.PublicUserRegistrationMaxUsers = 100;
            configuration.PublicUserRegistrationMaxActiveSessions = 2;
            configuration.PublicUserRegistrationRemoteClientBitrateLimit = 8_000_000;

            using var response = await client.PostAsJsonAsync("System/Configuration", configuration, _jsonOptions, TestContext.Current.CancellationToken);
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
            client.DefaultRequestHeaders.Remove(AuthHelper.AuthHeaderName);
            client.DefaultRequestHeaders.TryAddWithoutValidation(AuthHelper.AuthHeaderName, AuthHelper.DummyAuthHeader);
        }
    }
}

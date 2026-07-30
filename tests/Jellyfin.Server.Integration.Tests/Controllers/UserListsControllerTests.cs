using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Models.UserDtos;
using Jellyfin.Api.Models.UserListDtos;
using Jellyfin.Extensions.Json;
using MediaBrowser.Model.Dto;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public sealed class UserListsControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
    private static string? _accessToken;

    public UserListsControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetUserLists_NewUser_HasProvisionedDefaultWatchlist()
    {
        using var adminClient = await CreateAdminClientAsync();
        var user = await CreateUserAsync(adminClient, CreateUniqueName("provisioned"));
        using var userClient = await CreateUserClientAsync(user.Name);

        using var response = await userClient.GetAsync(
            "UserLists",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var lists = await response.Content.ReadFromJsonAsync<UserListDto[]>(
            _jsonOptions,
            TestContext.Current.CancellationToken);
        Assert.NotNull(lists);
        var defaultList = Assert.Single(lists, list => list.IsDefault);
        Assert.Equal("Watchlist", defaultList.Name);
    }

    [Fact]
    public async Task OtherUsersList_ReadAndMutation_ReturnNotFound()
    {
        using var adminClient = await CreateAdminClientAsync();
        var userA = await CreateUserAsync(adminClient, CreateUniqueName("user-a"));
        var userB = await CreateUserAsync(adminClient, CreateUniqueName("user-b"));
        Assert.False(userA.Policy.IsAdministrator);
        Assert.False(userB.Policy.IsAdministrator);
        using var userAClient = await CreateUserClientAsync(userA.Name);
        using var userBClient = await CreateUserClientAsync(userB.Name);

        var userBLists = await GetUserListsAsync(userBClient);
        var userBDefaultList = Assert.Single(userBLists, list => list.IsDefault);

        using var readResponse = await userAClient.GetAsync(
            $"UserLists/{userBDefaultList.Id}/Items",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, readResponse.StatusCode);

        using var mutationResponse = await userAClient.DeleteAsync(
            $"UserLists/{userBDefaultList.Id}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, mutationResponse.StatusCode);

        var userBListsAfterMutation = await GetUserListsAsync(userBClient);
        Assert.Contains(
            userBListsAfterMutation,
            list => list.Id.Equals(userBDefaultList.Id));
    }

    [Fact]
    public async Task DeleteUserList_DefaultList_ReturnsClientError()
    {
        using var adminClient = await CreateAdminClientAsync();
        var user = await CreateUserAsync(adminClient, CreateUniqueName("default-delete"));
        using var userClient = await CreateUserClientAsync(user.Name);
        var lists = await GetUserListsAsync(userClient);
        var defaultList = Assert.Single(lists, list => list.IsDefault);

        using var response = await userClient.DeleteAsync(
            $"UserLists/{defaultList.Id}",
            TestContext.Current.CancellationToken);

        Assert.InRange((int)response.StatusCode, 400, 499);
        var listsAfterDeletionAttempt = await GetUserListsAsync(userClient);
        Assert.Contains(
            listsAfterDeletionAttempt,
            list => list.Id.Equals(defaultList.Id));
    }

    [Fact]
    public async Task CreateUserList_DuplicateName_ReturnsClientError()
    {
        using var client = await CreateAdminClientAsync();
        var listName = CreateUniqueName("duplicate-list");
        var request = new CreateUserListDto
        {
            Name = listName
        };

        using var firstResponse = await client.PostAsJsonAsync(
            "UserLists",
            request,
            _jsonOptions,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);

        using var duplicateResponse = await client.PostAsJsonAsync(
            "UserLists",
            request,
            _jsonOptions,
            TestContext.Current.CancellationToken);

        Assert.InRange((int)duplicateResponse.StatusCode, 400, 499);
        Assert.NotEqual(HttpStatusCode.InternalServerError, duplicateResponse.StatusCode);
    }

    [Fact]
    public async Task GetUserLists_Unauthenticated_ReturnsUnauthorized()
    {
        using var authenticatedClient = await CreateAdminClientAsync();
        using var unauthenticatedClient = _factory.CreateClient();

        using var response = await unauthenticatedClient.GetAsync(
            "UserLists",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static string CreateUniqueName(string prefix)
    {
        return prefix + "-" + Guid.NewGuid().ToString("N");
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(
            _accessToken ??= await AuthHelper.CompleteStartupAsync(client));
        return client;
    }

    private async Task<HttpClient> CreateUserClientAsync(string username)
    {
        var client = _factory.CreateClient();
        var accessToken = await AuthenticateAsync(client, username);
        client.DefaultRequestHeaders.AddAuthHeader(accessToken);
        return client;
    }

    private async Task<UserDto> CreateUserAsync(HttpClient adminClient, string username)
    {
        using var response = await adminClient.PostAsJsonAsync(
            "Users/New",
            new CreateUserByName
            {
                Name = username
            },
            _jsonOptions,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<UserDto>(
            _jsonOptions,
            TestContext.Current.CancellationToken);
        Assert.NotNull(user);
        return user;
    }

    private async Task<UserListDto[]> GetUserListsAsync(HttpClient client)
    {
        using var response = await client.GetAsync(
            "UserLists",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var lists = await response.Content.ReadFromJsonAsync<UserListDto[]>(
            _jsonOptions,
            TestContext.Current.CancellationToken);
        Assert.NotNull(lists);
        return lists;
    }

    private async Task<string> AuthenticateAsync(HttpClient client, string username)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/Users/AuthenticateByName");
        request.Headers.TryAddWithoutValidation(
            AuthHelper.AuthHeaderName,
            AuthHelper.DummyAuthHeader);
        request.Content = JsonContent.Create(
            new AuthenticateUserByName
            {
                Username = username,
                Pw = string.Empty
            },
            options: _jsonOptions);

        using var response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var authentication = await response.Content.ReadFromJsonAsync<AuthenticationResponse>(
            _jsonOptions,
            TestContext.Current.CancellationToken);
        Assert.NotNull(authentication);
        return authentication.AccessToken;
    }

    private sealed class AuthenticationResponse
    {
        public string AccessToken { get; set; } = string.Empty;
    }
}

using System;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public sealed class UserLibraryControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
    private static string? _accessToken;

    public UserLibraryControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetRootFolder_NonExistenUserId_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

        var response = await client.GetAsync($"Users/{Guid.NewGuid()}/Items/Root").ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRootFolder_UserId_Valid()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

        _ = await AuthHelper.GetRootFolderDtoAsync(client).ConfigureAwait(false);
    }

    [Theory]
    [InlineData("Users/{0}/Items/{1}")]
    [InlineData("Users/{0}/Items/{1}/Intros")]
    [InlineData("Users/{0}/Items/{1}/LocalTrailers")]
    [InlineData("Users/{0}/Items/{1}/SpecialFeatures")]
    [InlineData("Users/{0}/Items/{1}/Lyrics")]
    public async Task GetItem_NonExistenUserId_NotFound(string format)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

        var rootFolderDto = await AuthHelper.GetRootFolderDtoAsync(client).ConfigureAwait(false);

        var response = await client.GetAsync(string.Format(CultureInfo.InvariantCulture, format, Guid.NewGuid(), rootFolderDto.Id)).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("Users/{0}/Items/{1}")]
    [InlineData("Users/{0}/Items/{1}/Intros")]
    [InlineData("Users/{0}/Items/{1}/LocalTrailers")]
    [InlineData("Users/{0}/Items/{1}/SpecialFeatures")]
    [InlineData("Users/{0}/Items/{1}/Lyrics")]
    public async Task GetItem_NonExistentItemId_NotFound(string format)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

        var userDto = await AuthHelper.GetUserDtoAsync(client).ConfigureAwait(false);

        var response = await client.GetAsync(string.Format(CultureInfo.InvariantCulture, format, userDto.Id, Guid.NewGuid())).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetItem_UserIdAndItemId_Valid()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

        var userDto = await AuthHelper.GetUserDtoAsync(client).ConfigureAwait(false);
        var rootFolderDto = await AuthHelper.GetRootFolderDtoAsync(client, userDto.Id).ConfigureAwait(false);

        var response = await client.GetAsync($"Users/{userDto.Id}/Items/{rootFolderDto.Id}").ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rootDto = await JsonSerializer.DeserializeAsync<BaseItemDto>(
                    await response.Content.ReadAsStreamAsync().ConfigureAwait(false),
                    _jsonOptions).ConfigureAwait(false);
        Assert.NotNull(rootDto);
    }

    [Fact]
    public async Task GetIntros_UserIdAndItemId_Valid()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

        var userDto = await AuthHelper.GetUserDtoAsync(client).ConfigureAwait(false);
        var rootFolderDto = await AuthHelper.GetRootFolderDtoAsync(client, userDto.Id).ConfigureAwait(false);

        var response = await client.GetAsync($"Users/{userDto.Id}/Items/{rootFolderDto.Id}/Intros").ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rootDto = await JsonSerializer.DeserializeAsync<QueryResult<BaseItemDto>>(
                    await response.Content.ReadAsStreamAsync().ConfigureAwait(false),
                    _jsonOptions).ConfigureAwait(false);
        Assert.NotNull(rootDto);
    }

    [Theory]
    [InlineData("Users/{0}/Items/{1}/LocalTrailers")]
    [InlineData("Users/{0}/Items/{1}/SpecialFeatures")]
    public async Task LocalTrailersAndSpecialFeatures_UserIdAndItemId_Valid(string format)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

        var userDto = await AuthHelper.GetUserDtoAsync(client).ConfigureAwait(false);
        var rootFolderDto = await AuthHelper.GetRootFolderDtoAsync(client, userDto.Id).ConfigureAwait(false);

        var response = await client.GetAsync(string.Format(CultureInfo.InvariantCulture, format, userDto.Id, rootFolderDto.Id)).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rootDto = await JsonSerializer.DeserializeAsync<BaseItemDto[]>(
                    await response.Content.ReadAsStreamAsync().ConfigureAwait(false),
                    _jsonOptions).ConfigureAwait(false);
        Assert.NotNull(rootDto);
    }
}

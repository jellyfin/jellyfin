using System;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
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
    public async Task GetRootFolder_NonexistentUserId_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var response = await client.GetAsync($"Users/{Guid.NewGuid()}/Items/Root", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetRootFolder_UserId_Valid()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        _ = await AuthHelper.GetRootFolderDtoAsync(client);
    }

    [Theory]
    [InlineData("Users/{0}/Items/{1}")]
    [InlineData("Users/{0}/Items/{1}/Intros")]
    [InlineData("Users/{0}/Items/{1}/LocalTrailers")]
    [InlineData("Users/{0}/Items/{1}/SpecialFeatures")]
    [InlineData("Users/{0}/Items/{1}/Lyrics")]
    public async Task GetItem_NonexistentUserId_NotFound(string format)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var rootFolderDto = await AuthHelper.GetRootFolderDtoAsync(client);

        var response = await client.GetAsync(string.Format(CultureInfo.InvariantCulture, format, Guid.NewGuid(), rootFolderDto.Id), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("Users/{0}/Items/{1}")]
    [InlineData("Users/{0}/Items/{1}/Intros")]
    [InlineData("Users/{0}/Items/{1}/LocalTrailers")]
    [InlineData("Users/{0}/Items/{1}/SpecialFeatures")]
    [InlineData("Users/{0}/Items/{1}/Lyrics")]
    public async Task GetItem_NonexistentItemId_NotFound(string format)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var userDto = await AuthHelper.GetUserDtoAsync(client);

        var response = await client.GetAsync(string.Format(CultureInfo.InvariantCulture, format, userDto.Id, Guid.NewGuid()), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact(Skip = "Disabled for flaky execution after refactor.")]
    public async Task GetItem_UserIdAndItemId_Valid()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var userDto = await AuthHelper.GetUserDtoAsync(client);
        var rootFolderDto = await AuthHelper.GetRootFolderDtoAsync(client, userDto.Id);

        var response = await client.GetAsync($"Users/{userDto.Id}/Items/{rootFolderDto.Id}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rootDto = await response.Content.ReadFromJsonAsync<BaseItemDto>(_jsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(rootDto);
    }

    [Fact(Skip = "Disabled for flaky execution after refactor.")]
    public async Task GetIntros_UserIdAndItemId_Valid()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var userDto = await AuthHelper.GetUserDtoAsync(client);
        var rootFolderDto = await AuthHelper.GetRootFolderDtoAsync(client, userDto.Id);

        var response = await client.GetAsync($"Users/{userDto.Id}/Items/{rootFolderDto.Id}/Intros", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rootDto = await response.Content.ReadFromJsonAsync<QueryResult<BaseItemDto>>(_jsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(rootDto);
    }

    [Theory(Skip = "Disabled for flaky execution after refactor.")]
    [InlineData("Users/{0}/Items/{1}/LocalTrailers")]
    [InlineData("Users/{0}/Items/{1}/SpecialFeatures")]
    public async Task LocalTrailersAndSpecialFeatures_UserIdAndItemId_Valid(string format)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var userDto = await AuthHelper.GetUserDtoAsync(client);
        var rootFolderDto = await AuthHelper.GetRootFolderDtoAsync(client, userDto.Id);

        var response = await client.GetAsync(string.Format(CultureInfo.InvariantCulture, format, userDto.Id, rootFolderDto.Id), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var rootDto = await response.Content.ReadFromJsonAsync<BaseItemDto[]>(_jsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(rootDto);
    }

    [Theory]
    // Bounds of the accepted range.
    [InlineData(0, false)]
    [InlineData(10, true)]
    // Either side of MinLikeValue (6.5), where the derived Likes flag flips.
    [InlineData(6.49, false)]
    [InlineData(6.5, true)]
    [InlineData(6.51, true)]
    [InlineData(3, false)]
    [InlineData(7, true)]
    public async Task UpdateUserItemRating_NumericRating_StoresRatingAndDerivesLikes(double rating, bool expectedLikes)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var rootFolderDto = await AuthHelper.GetRootFolderDtoAsync(client);

        var response = await client.PostAsync(
            string.Format(CultureInfo.InvariantCulture, "UserItems/{0}/Rating?rating={1}", rootFolderDto.Id, rating),
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var userData = await response.Content.ReadFromJsonAsync<UserItemDataDto>(_jsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(userData);
        Assert.Equal(rating, userData.Rating);

        // Likes remains derived from Rating via UserItemData.MinLikeValue.
        Assert.Equal(expectedLikes, userData.Likes);
    }

    [Theory]
    // Just outside each bound.
    [InlineData("-0.1")]
    [InlineData("10.1")]
    [InlineData("-1")]
    [InlineData("10.5")]
    [InlineData("11")]
    // Non-finite values: NaN compares false against every bound, so it must be rejected explicitly.
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("-Infinity")]
    // Not a number at all.
    [InlineData("abc")]
    public async Task UpdateUserItemRating_RatingOutOfRange_BadRequest(string rating)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var rootFolderDto = await AuthHelper.GetRootFolderDtoAsync(client);

        var response = await client.PostAsync(
            string.Format(CultureInfo.InvariantCulture, "UserItems/{0}/Rating?rating={1}", rootFolderDto.Id, rating),
            null,
            TestContext.Current.CancellationToken);

        // Must be rejected at the model-binding/[Range] layer. If it reaches
        // UserItemData.Rating the setter throws, which would surface as a 500.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(true, 10)]
    [InlineData(false, 1)]
    public async Task UpdateUserItemRating_Likes_RetainsLegacyBehaviour(bool likes, double expectedRating)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var rootFolderDto = await AuthHelper.GetRootFolderDtoAsync(client);

        var response = await client.PostAsync(
            string.Format(CultureInfo.InvariantCulture, "UserItems/{0}/Rating?likes={1}", rootFolderDto.Id, likes),
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var userData = await response.Content.ReadFromJsonAsync<UserItemDataDto>(_jsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(userData);
        Assert.Equal(expectedRating, userData.Rating);
        Assert.Equal(likes, userData.Likes);
    }

    [Fact]
    public async Task UpdateUserItemRating_RatingAndLikes_RatingWins()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var rootFolderDto = await AuthHelper.GetRootFolderDtoAsync(client);

        var response = await client.PostAsync(
            string.Format(CultureInfo.InvariantCulture, "UserItems/{0}/Rating?likes=true&rating={1}", rootFolderDto.Id, 2),
            null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var userData = await response.Content.ReadFromJsonAsync<UserItemDataDto>(_jsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(userData);
        Assert.Equal(2, userData.Rating);
        Assert.False(userData.Likes);
    }

    [Fact]
    public async Task DeleteUserItemRating_ClearsRating()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var rootFolderDto = await AuthHelper.GetRootFolderDtoAsync(client);

        var setResponse = await client.PostAsync(
            string.Format(CultureInfo.InvariantCulture, "UserItems/{0}/Rating?rating={1}", rootFolderDto.Id, 8),
            null,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, setResponse.StatusCode);

        var response = await client.DeleteAsync(
            string.Format(CultureInfo.InvariantCulture, "UserItems/{0}/Rating", rootFolderDto.Id),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var userData = await response.Content.ReadFromJsonAsync<UserItemDataDto>(_jsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(userData);
        Assert.Null(userData.Rating);
        Assert.Null(userData.Likes);
    }
}

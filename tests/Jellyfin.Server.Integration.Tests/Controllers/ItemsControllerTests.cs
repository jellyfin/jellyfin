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

public sealed class ItemsControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
    private static string? _accessToken;

    public ItemsControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetItems_NoApiKeyOrUserId_Success()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

        var response = await client.GetAsync("Items").ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("Users/{0}/Items")]
    [InlineData("Users/{0}/Items/Resume")]
    public async Task GetUserItems_NonExistentUserId_NotFound(string format)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

        var response = await client.GetAsync(string.Format(CultureInfo.InvariantCulture, format, Guid.NewGuid())).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("Items?userId={0}")]
    [InlineData("Users/{0}/Items")]
    [InlineData("Users/{0}/Items/Resume")]
    public async Task GetItems_UserId_Ok(string format)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

        var userDto = await AuthHelper.GetUserDtoAsync(client).ConfigureAwait(false);

        var response = await client.GetAsync(string.Format(CultureInfo.InvariantCulture, format, userDto.Id)).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await JsonSerializer.DeserializeAsync<QueryResult<BaseItemDto>>(
                    await response.Content.ReadAsStreamAsync().ConfigureAwait(false),
                    _jsonOptions).ConfigureAwait(false);
        Assert.NotNull(items);
    }
}

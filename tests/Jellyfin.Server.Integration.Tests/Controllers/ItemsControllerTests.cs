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

[Collection("Controller collection")]
public sealed class ItemsControllerTests
{
    private readonly JellyfinApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;

    public ItemsControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetItems_NoApiKeyOrUserId_Success()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(await _factory.GetAccessTokenAsync());

        var response = await client.GetAsync("Items", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("Users/{0}/Items")]
    [InlineData("Users/{0}/Items/Resume")]
    public async Task GetUserItems_NonexistentUserId_NotFound(string format)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(await _factory.GetAccessTokenAsync());

        var response = await client.GetAsync(string.Format(CultureInfo.InvariantCulture, format, Guid.NewGuid()), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("Items?userId={0}")]
    [InlineData("Users/{0}/Items")]
    [InlineData("Users/{0}/Items/Resume")]
    public async Task GetItems_UserId_Ok(string format)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(await _factory.GetAccessTokenAsync());

        var userDto = await AuthHelper.GetUserDtoAsync(client);

        var response = await client.GetAsync(string.Format(CultureInfo.InvariantCulture, format, userDto.Id), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var items = await response.Content.ReadFromJsonAsync<QueryResult<BaseItemDto>>(_jsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(items);
    }
}

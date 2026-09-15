using System;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

[Collection("Controller collection")]
public class PlaystateControllerTests
{
    private readonly JellyfinApplicationFactory _factory;

    public PlaystateControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task DeleteMarkUnplayedItem_NonexistentUserId_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(await _factory.GetAccessTokenAsync());

        using var response = await client.DeleteAsync($"Users/{Guid.NewGuid()}/PlayedItems/{Guid.NewGuid()}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostMarkPlayedItem_NonexistentUserId_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(await _factory.GetAccessTokenAsync());

        using var response = await client.PostAsync($"Users/{Guid.NewGuid()}/PlayedItems/{Guid.NewGuid()}", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteMarkUnplayedItem_NonexistentItemId_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(await _factory.GetAccessTokenAsync());

        var userDto = await AuthHelper.GetUserDtoAsync(client);

        using var response = await client.DeleteAsync($"Users/{userDto.Id}/PlayedItems/{Guid.NewGuid()}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PostMarkPlayedItem_NonexistentItemId_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(await _factory.GetAccessTokenAsync());

        var userDto = await AuthHelper.GetUserDtoAsync(client);

        using var response = await client.PostAsync($"Users/{userDto.Id}/PlayedItems/{Guid.NewGuid()}", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

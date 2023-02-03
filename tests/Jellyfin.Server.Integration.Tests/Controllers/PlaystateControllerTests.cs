using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using Xunit.Priority;

namespace Jellyfin.Server.Integration.Tests.Controllers;

[TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
public class PlaystateControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;
    private static readonly Guid _testUserId = Guid.NewGuid();
    private static readonly Guid _testItemId = Guid.NewGuid();
    private static string? _accessToken;

    public PlaystateControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    private Task<HttpResponseMessage> DeleteUserPlayedItems(HttpClient httpClient, Guid userId, Guid itemId)
        => httpClient.DeleteAsync($"Users/{userId}/PlayedItems/{itemId}");

    private Task<HttpResponseMessage> PostUserPlayedItems(HttpClient httpClient, Guid userId, Guid itemId)
        => httpClient.PostAsync($"Users/{userId}/PlayedItems/{itemId}", null);

    [Fact]
    [Priority(0)]
    public async Task DeleteMarkUnplayedItem_DoesNotExist_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

        using var response = await DeleteUserPlayedItems(client, _testUserId, _testItemId).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    [Priority(0)]
    public async Task PostMarkPlayedItem_DoesNotExist_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client).ConfigureAwait(false));

        using var response = await PostUserPlayedItems(client, _testUserId, _testItemId).ConfigureAwait(false);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

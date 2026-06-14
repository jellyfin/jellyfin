using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Jellyfin.Api.Models.PlaylistDtos;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions.Json;
using MediaBrowser.Model.Playlists;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public sealed class PlaylistsControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;
    private static string? _accessToken;

    public PlaylistsControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MoveItem_OutOfRangeIndex_DoesNotReturnServerError()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        // Create an empty playlist owned by the authenticated user (no media items required).
        using var createResponse = await client.PostAsJsonAsync(
            "Playlists",
            new CreatePlaylistDto { Name = "MoveItemRepro", MediaType = MediaType.Audio },
            JsonDefaults.Options,
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);

        var created = await createResponse.Content
            .ReadFromJsonAsync<PlaylistCreationResult>(JsonDefaults.Options, TestContext.Current.CancellationToken);
        Assert.NotNull(created);

        // Regression test for #17066: moving an item to an out-of-range index used to throw an
        // unhandled IndexOutOfRangeException in PlaylistManager.MoveItemAsync (HTTP 500) because
        // newIndex was used to index the items array without bound-checking.
        using var moveResponse = await client.PostAsync(
            $"Playlists/{created.Id}/Items/{Guid.Empty:N}/Move/999",
            content: null,
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, moveResponse.StatusCode);
    }
}

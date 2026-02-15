using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

[Collection("Controller collection")]
public sealed class MusicGenreControllerTests
{
    private readonly JellyfinApplicationFactory _factory;
    private static string? _accessToken;

    public MusicGenreControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MusicGenres_FakeMusicGenre_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var response = await client.GetAsync("MusicGenres/Fake-MusicGenre");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

[Collection("Controller collection")]
public sealed class MusicGenreControllerTests
{
    private readonly JellyfinApplicationFactory _factory;

    public MusicGenreControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task MusicGenres_FakeMusicGenre_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(await _factory.GetAccessTokenAsync());

        var response = await client.GetAsync("MusicGenres/Fake-MusicGenre", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

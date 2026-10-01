using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public sealed class GenresControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;
    private static string? _accessToken;

    public GenresControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetGenre_DoesntExist_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        using var response = await client.GetAsync("Genres/DoesntExist", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

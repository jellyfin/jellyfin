using System;
using System.Globalization;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public sealed class LibraryControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;
    private static string? _accessToken;

    public LibraryControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Items/{0}/File")]
    [InlineData("Items/{0}/ThemeSongs")]
    [InlineData("Items/{0}/ThemeVideos")]
    [InlineData("Items/{0}/ThemeMedia")]
    [InlineData("Items/{0}/Ancestors")]
    [InlineData("Items/{0}/Download")]
    [InlineData("Artists/{0}/Similar")]
    [InlineData("Items/{0}/Similar")]
    [InlineData("Albums/{0}/Similar")]
    [InlineData("Shows/{0}/Similar")]
    [InlineData("Movies/{0}/Similar")]
    [InlineData("Trailers/{0}/Similar")]
    public async Task Get_NonexistentItemId_NotFound(string format)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var response = await client.GetAsync(string.Format(CultureInfo.InvariantCulture, format, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("Items/{0}")]
    [InlineData("Items?ids={0}")]
    public async Task Delete_NonexistentItemId_Unauthorised(string format)
    {
        var client = _factory.CreateClient();

        var response = await client.DeleteAsync(string.Format(CultureInfo.InvariantCulture, format, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Items/{0}")]
    [InlineData("Items?ids={0}")]
    public async Task Delete_NonexistentItemId_NotFound(string format)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var response = await client.DeleteAsync(string.Format(CultureInfo.InvariantCulture, format, Guid.NewGuid()));
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

[Collection("Controller collection")]
public class PersonsControllerTests
{
    private readonly JellyfinApplicationFactory _factory;
    private static string? _accessToken;

    public PersonsControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetPerson_DoesntExist_NotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        using var response = await client.GetAsync($"Persons/DoesntExist");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Api.Controllers;
using Jellyfin.Server.Implementations;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public class DisplayPreferencesControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;
    private static string? _accessToken;

    public DisplayPreferencesControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void ClientParameters_StringLength_IsTheLengthOfTheColumn()
    {
        var lengths = typeof(DisplayPreferencesController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .SelectMany(e => e.GetParameters())
            .Where(e => e.Name == "client")
            .Select(e => e.GetCustomAttribute<StringLengthAttribute>()?.MaximumLength)
            .ToList();

        Assert.Equal(2, lengths.Count);
        Assert.All(lengths, length => Assert.Equal(KeyNormalizer.DisplayPreferencesClientMaxLength, length));
    }

    [Fact]
    public async Task GetDisplayPreferences_ClientAtTheColumnLength_Success()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        using var response = await client.GetAsync($"DisplayPreferences/usersettings?client={new string('c', KeyNormalizer.DisplayPreferencesClientMaxLength)}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetDisplayPreferences_ClientLongerThanTheColumn_BadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        using var response = await client.GetAsync($"DisplayPreferences/usersettings?client={new string('c', KeyNormalizer.DisplayPreferencesClientMaxLength + 1)}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UpdateDisplayPreferences_ClientLongerThanTheColumn_BadRequest()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        using var content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await client.PostAsync($"DisplayPreferences/usersettings?client={new string('c', KeyNormalizer.DisplayPreferencesClientMaxLength + 1)}", content, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

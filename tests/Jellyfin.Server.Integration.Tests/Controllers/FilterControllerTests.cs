using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public sealed class FilterControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;
    private static string? _accessToken;

    public FilterControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("Movie")]
    [InlineData("Series")]
    public async Task GetQueryFilters_EmptyLibrary_SerializesEmptyArrays(string itemType)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var filters = await client.GetFromJsonAsync<JsonElement>($"/Items/Filters2?includeItemTypes={itemType}", TestContext.Current.CancellationToken);

        foreach (var field in new[] { "Years", "OfficialRatings", "Genres", "Tags", "AudioLanguages", "SubtitleLanguages" })
        {
            var values = filters.GetProperty(field);
            Assert.Equal(JsonValueKind.Array, values.ValueKind);
            Assert.Equal(0, values.GetArrayLength());
        }
    }
}

using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

/// <summary>
/// Tests that the studio endpoints kept for backwards compatibility still answer, and answer with
/// companies of kind Studio.
/// </summary>
public sealed class StudiosControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;
    private static string? _accessToken;

    public StudiosControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetStudios_NoStudios_EmptyResult()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        using var response = await client.GetAsync("Studios", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var studios = await response.Content.ReadFromJsonAsync<QueryResult<BaseItemDto>>(JsonDefaults.Options, TestContext.Current.CancellationToken);
        Assert.NotNull(studios);
        Assert.Empty(studios.Items);
    }

    [Fact]
    public async Task GetStudio_ByName_ReturnsTheStudioKindCompany()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        using var studioResponse = await client.GetAsync("Studios/Bad%20Robot", TestContext.Current.CancellationToken);
        using var companyResponse = await client.GetAsync("Companies/Bad%20Robot?companyType=Studio", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, studioResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, companyResponse.StatusCode);

        var studio = await studioResponse.Content.ReadFromJsonAsync<BaseItemDto>(JsonDefaults.Options, TestContext.Current.CancellationToken);
        var company = await companyResponse.Content.ReadFromJsonAsync<BaseItemDto>(JsonDefaults.Options, TestContext.Current.CancellationToken);

        Assert.NotNull(studio);
        Assert.NotNull(company);
        Assert.Equal(company.Id, studio.Id);
    }

    [Theory]
    [InlineData("Items?recursive=true&studios=Bad%20Robot")]
    [InlineData("Items?recursive=true&studioIds=8d5e0bd1-1a2a-4b7e-9b5d-3e7a2c9f4a11")]
    [InlineData("Artists?studios=Bad%20Robot")]
    [InlineData("Artists/AlbumArtists?studios=Bad%20Robot")]
    public async Task LegacyStudioFilter_IsStillBound_Ok(string url)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        using var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LegacyStudioFilter_OnUserItems_IsStillBound_Ok()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var userDto = await AuthHelper.GetUserDtoAsync(client);
        var url = string.Format(CultureInfo.InvariantCulture, "Users/{0}/Items?recursive=true&studios=Bad%20Robot", userDto.Id);

        using var response = await client.GetAsync(url, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

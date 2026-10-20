using System;
using System.Net;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.Model.LiveTv;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public sealed class LiveTvControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;
    private readonly JsonSerializerOptions _jsonOptions = JsonDefaults.Options;
    private static string? _accessToken;

    public LiveTvControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddTunerHost_Unauthorized_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var body = new TunerHostInfo()
        {
            Type = "m3u",
            Url = "Test Data/dummy.m3u8"
        };

        var response = await client.PostAsJsonAsync("/LiveTv/TunerHosts", body, _jsonOptions, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AddTunerHost_Valid_ReturnsCorrectResponse()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var body = new TunerHostInfo()
        {
            Type = "m3u",
            Url = "Test Data/dummy.m3u8"
        };

        var response = await client.PostAsJsonAsync("/LiveTv/TunerHosts", body, _jsonOptions, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MediaTypeNames.Application.Json, response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(Encoding.UTF8.BodyName, response.Content.Headers.ContentType?.CharSet);
        var responseBody = await response.Content.ReadFromJsonAsync<TunerHostInfo>(TestContext.Current.CancellationToken);
        Assert.NotNull(responseBody);
        Assert.Equal(body.Type, responseBody.Type);
        Assert.Equal(body.Url, responseBody.Url);
    }

    [Fact]
    public async Task AddTunerHost_InvalidType_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var body = new TunerHostInfo()
        {
            Type = "invalid",
            Url = "Test Data/dummy.m3u8"
        };

        var response = await client.PostAsJsonAsync("/LiveTv/TunerHosts", body, _jsonOptions, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddTunerHost_InvalidUrl_ReturnsNotFound()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var body = new TunerHostInfo()
        {
            Type = "m3u",
            Url = "thisgoesnowhere"
        };

        var response = await client.PostAsJsonAsync("/LiveTv/TunerHosts", body, _jsonOptions, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task LiveTvInstance_CanBeCreatedUpdatedAndDeleted()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        var name = "Live TV test " + Guid.NewGuid().ToString("N");
        var instance = new LiveTvInstanceInfo
        {
            Name = name
        };

        var createResponse = await client.PostAsJsonAsync("/LiveTv/Instances", instance, _jsonOptions, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        var createdInstance = await createResponse.Content.ReadFromJsonAsync<LiveTvInstanceInfo>(_jsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(createdInstance);
        Assert.NotEqual(Guid.Empty, createdInstance.Id);
        Assert.Equal(name, createdInstance.Name);

        var updateName = name + " updated";
        createdInstance.Name = updateName;
        var updateResponse = await client.PostAsJsonAsync("/LiveTv/Instances", createdInstance, _jsonOptions, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var instancesResponse = await client.GetFromJsonAsync<LiveTvInstanceInfo[]>("/LiveTv/Instances", _jsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(instancesResponse);
        Assert.Contains(instancesResponse, i => i.Id.Equals(createdInstance.Id) && i.Name == updateName);

        var deleteResponse = await client.DeleteAsync($"/LiveTv/Instances/{createdInstance.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        var remainingInstances = await client.GetFromJsonAsync<LiveTvInstanceInfo[]>("/LiveTv/Instances", _jsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(remainingInstances);
        Assert.DoesNotContain(remainingInstances, i => i.Id.Equals(createdInstance.Id));
    }
}

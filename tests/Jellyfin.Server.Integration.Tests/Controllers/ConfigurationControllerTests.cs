using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using Jellyfin.Server.Extensions;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.LiveTv;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public sealed class ConfigurationControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;
    private static string? _accessToken;

    public ConfigurationControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetConfiguration_Unauthorized_Unauthorized()
    {
        var client = _factory.CreateClient();

        using var request = new HttpRequestMessage(HttpMethod.Get, GetUrl("encoding"));
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateConfiguration_CamelCaseProperties_AreApplied()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        await PostConfiguration(client, "encoding", """{"h264Crf":18,"enableHardwareEncoding":false}""");

        using var request = new HttpRequestMessage(HttpMethod.Get, GetUrl("encoding"));
        request.Headers.Accept.ParseAdd(JsonDefaults.CamelCaseMediaType);
        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.NotNull(body);
        Assert.NotNull(body["h264Crf"]);
        Assert.Equal(18, body["h264Crf"]!.GetValue<int>());
    }

    [Theory]
    [InlineData("encoding", "null")]
    [InlineData("encoding", """{"H264Crf":null}""")]
    [InlineData("network", """{"InternalHttpPort":"invalid"}""")]
    [InlineData("network", """{"BaseUrl":null}""")]
    [InlineData("network", """{"LocalNetworkSubnets":null}""")]
    [InlineData("network", """{"LocalNetworkAddresses":null}""")]
    [InlineData("network", """{"KnownProxies":null}""")]
    [InlineData("network", """{"VirtualInterfaceNames":null}""")]
    [InlineData("network", """{"PublishedServerUriBySubnet":null}""")]
    [InlineData("network", """{"RemoteIPFilter":null}""")]
    [InlineData("livetv", """{"TunerHosts":null}""")]
    [InlineData("livetv", """{"ListingProviders":null}""")]
    [InlineData("livetv", """{"MediaLocationsCreated":null}""")]
    [InlineData("livetv", """{"RecordingPostProcessorArguments":null}""")]
    [InlineData("xbmcmetadata", """{"ReleaseDateFormat":null}""")]
    [InlineData("", """{"ServerName":null}""")]
    public async Task UpdateConfiguration_InvalidRequest_DoesNotSave(string key, string json)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));

        using var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
        using var response = await client.PostAsync(GetUrl(key), content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static string GetUrl(string key)
    {
        return string.IsNullOrEmpty(key) ? "/System/Configuration" : "/System/Configuration/" + key;
    }

    private async Task PostConfiguration(HttpClient client, string key, string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
        using var response = await client.PostAsync(GetUrl(key), content, TestContext.Current.CancellationToken);
        Assert.True(
            response.StatusCode == HttpStatusCode.NoContent,
            $"{response.StatusCode}: {await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");
    }
}

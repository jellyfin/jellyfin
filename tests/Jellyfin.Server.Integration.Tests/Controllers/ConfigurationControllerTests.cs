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

public sealed class ConfigurationControllerTests : IDisposable
{
    private readonly Dictionary<string, object> _configurations = new()
    {
        [string.Empty] = new ServerConfiguration(),
        ["encoding"] = new EncodingOptions(),
        ["network"] = new NetworkConfiguration(),
        ["metadata"] = new MetadataConfiguration(),
        ["xbmcmetadata"] = new XbmcMetadataOptions(),
        ["livetv"] = new LiveTvOptions()
    };

    private readonly Mock<IServerConfigurationManager> _configurationManager = new();
    private readonly IHost _host;
    private readonly HttpClient _client;

    public ConfigurationControllerTests()
    {
        _configurationManager.Setup(manager => manager.Configuration)
            .Returns(() => (ServerConfiguration)_configurations[string.Empty]);
        _configurationManager.Setup(manager => manager.ReplaceConfiguration(It.IsAny<BaseApplicationConfiguration>()))
            .Callback<BaseApplicationConfiguration>(configuration => _configurations[string.Empty] = configuration);
        _configurationManager.Setup(manager => manager.GetConfiguration(It.IsAny<string>()))
            .Returns((string key) => _configurations[key]);
        _configurationManager.Setup(manager => manager.SaveConfiguration(It.IsAny<string>(), It.IsAny<object>()))
            .Callback<string, object>((key, configuration) => _configurations[key] = configuration);

        // Use the real API setup, but keep config in memory for these tests.
        _host = new HostBuilder().ConfigureWebHost(webHost => webHost
            .UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddSingleton(_configurationManager.Object);
                services.AddAuthentication();
                services.AddAuthorization(options =>
                {
                    options.AddPolicy(Policies.RequiresElevation, policy => policy.RequireAuthenticatedUser());
                });
                services.AddJellyfinApi(Array.Empty<Assembly>(), new NetworkConfiguration());
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.Use((context, next) =>
                {
                    context.User = new ClaimsPrincipal(new ClaimsIdentity("Test"));
                    return next(context);
                });
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            })).Start();
        _client = _host.GetTestClient();
    }

    [Theory]
    [InlineData("")]
    [InlineData("encoding")]
    [InlineData("network")]
    [InlineData("metadata")]
    [InlineData("xbmcmetadata")]
    [InlineData("livetv")]
    public async Task UpdateConfiguration_EmptyRequest_UsesInternalDefaults(string key)
    {
        await AssertMatchesLegacyDeserialization(key, "{}");
    }

    [Theory]
    [InlineData("", """
        {"ActivityLogRetentionDays":null,"QuickConnectAvailable":false,"MinResumePct":0,
         "SortRemoveWords":[],"TrickplayOptions":{"EnableHwAcceleration":true}}
        """)]
    [InlineData("encoding", """
        {"EnableHardwareEncoding":false,"EncodingThreadCount":0,"HardwareDecodingCodecs":[],
         "H264Crf":"19","EncoderPreset":"slow","VaapiDevice":null,"UnknownOption":true}
        """)]
    [InlineData("network", """
        {"CertificatePath":null,"CertificatePassword":null,"InternalHttpPort":"8097",
         "EnableRemoteAccess":false,"BaseUrl":"jellyfin/","KnownProxies":[]}
        """)]
    [InlineData("network", """
        {"BaseUrl":"","LocalNetworkSubnets":[],"LocalNetworkAddresses":[],"KnownProxies":[],
         "VirtualInterfaceNames":[],"PublishedServerUriBySubnet":[],"RemoteIPFilter":[]}
        """)]
    [InlineData("metadata", """{"UseFileCreationTimeForDateAdded":false}""")]
    [InlineData("xbmcmetadata", """{"UserId":"","ReleaseDateFormat":"yyyy-MM-dd","SaveImagePathsInNfo":false}""")]
    [InlineData("livetv", """
        {"GuideDays":"","RecordingPath":null,"SaveRecordingNFO":false,
         "TunerHosts":[{"Url":"http://tuner"}],"ListingProviders":[{"Type":"XmlTv"}]}
        """)]
    [InlineData("livetv", """{"TunerHosts":[],"ListingProviders":[],"MediaLocationsCreated":[]}""")]
    public async Task UpdateConfiguration_SuppliedProperties_MatchLegacyDeserialization(string key, string json)
    {
        await AssertMatchesLegacyDeserialization(key, json);
    }

    [Theory]
    [InlineData("", "ServerName", "\"My server\"")]
    [InlineData("encoding", "H264Crf", "18")]
    [InlineData("network", "InternalHttpPort", "8097")]
    [InlineData("metadata", "UseFileCreationTimeForDateAdded", "false")]
    [InlineData("xbmcmetadata", "SaveImagePathsInNfo", "false")]
    [InlineData("livetv", "GuideDays", "7")]
    public async Task GetAndUpdateConfiguration_LowercaseRoute_RoundTripsConfiguration(string key, string property, string value)
    {
        using var response = await _client.GetAsync(GetUrl(key), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(MediaTypeNames.Application.Json, response.Content.Headers.ContentType?.MediaType);

        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken))!.AsObject();
        var expected = JsonSerializer.SerializeToNode(_configurations[key], JsonDefaults.Options)!.AsObject();
        Assert.True(JsonNode.DeepEquals(expected, body));

        body[property] = JsonNode.Parse(value);
        expected[property] = JsonNode.Parse(value);
        await PostConfiguration(key, body.ToJsonString());

        Assert.True(JsonNode.DeepEquals(expected, JsonSerializer.SerializeToNode(_configurations[key], JsonDefaults.Options)));
    }

    [Fact]
    public async Task UpdateConfiguration_CamelCaseProperties_AreApplied()
    {
        await PostConfiguration("encoding", """{"h264Crf":18,"enableHardwareEncoding":false}""");

        var configuration = Assert.IsType<EncodingOptions>(_configurations["encoding"]);
        Assert.Equal(18, configuration.H264Crf);
        Assert.False(configuration.EnableHardwareEncoding);
        Assert.Equal(new EncodingOptions().H265Crf, configuration.H265Crf);

        using var request = new HttpRequestMessage(HttpMethod.Get, GetUrl("encoding"));
        request.Headers.Accept.ParseAdd(JsonDefaults.CamelCaseMediaType);
        using var response = await _client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = JsonNode.Parse(await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.Equal(18, body!["h264Crf"]!.GetValue<int>());
    }

    [Fact]
    public async Task UpdateConfiguration_OmittedProperties_ResetToDefaultsRatherThanCurrentValues()
    {
        await PostConfiguration("encoding", """{"H264Crf":18,"EnableHardwareEncoding":false}""");
        await AssertMatchesLegacyDeserialization("encoding", "{}");
    }

    [Fact]
    public async Task UpdateConfiguration_DefaultObjects_AreNotSharedBetweenRequests()
    {
        await PostConfiguration(string.Empty, "{}");
        var first = Assert.IsType<ServerConfiguration>(_configurations[string.Empty]);
        first.MetadataOptions[0].ItemType = "Changed";
        first.TrickplayOptions.WidthResolutions[0] = 123;

        await AssertMatchesLegacyDeserialization(string.Empty, "{}");
        var second = Assert.IsType<ServerConfiguration>(_configurations[string.Empty]);
        Assert.NotSame(first.MetadataOptions, second.MetadataOptions);
        Assert.NotSame(first.TrickplayOptions, second.TrickplayOptions);
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
        using var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
        using var response = await _client.PostAsync(GetUrl(key), content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        _configurationManager.Verify(manager => manager.SaveConfiguration(It.IsAny<string>(), It.IsAny<object>()), Times.Never);
        _configurationManager.Verify(manager => manager.ReplaceConfiguration(It.IsAny<BaseApplicationConfiguration>()), Times.Never);
    }

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
    }

    private static string GetUrl(string key)
    {
        return string.IsNullOrEmpty(key) ? "/System/Configuration" : "/System/Configuration/" + key;
    }

    private async Task AssertMatchesLegacyDeserialization(string key, string json)
    {
        // Compare with how the old endpoint read the same JSON.
        var type = _configurations[key].GetType();
        var expected = JsonSerializer.Deserialize(json, type, JsonDefaults.Options);

        await PostConfiguration(key, json);

        var actual = _configurations[key];
        Assert.IsType(type, actual);
        Assert.True(JsonNode.DeepEquals(
            JsonSerializer.SerializeToNode(expected, type, JsonDefaults.Options),
            JsonSerializer.SerializeToNode(actual, type, JsonDefaults.Options)));
    }

    private async Task PostConfiguration(string key, string json)
    {
        using var content = new StringContent(json, Encoding.UTF8, MediaTypeNames.Application.Json);
        using var response = await _client.PostAsync(GetUrl(key), content, TestContext.Current.CancellationToken);
        Assert.True(
            response.StatusCode == HttpStatusCode.NoContent,
            $"{response.StatusCode}: {await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken)}");
    }
}

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Api.Auth;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Data;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Extensions;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Streaming;
using MediaBrowser.Controller.SyncPlay;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Auth;

public sealed class PlaybackAccessTests : IDisposable
{
    private readonly PlaybackAccessGrant _grant;
    private readonly IHost _server;
    private readonly HttpClient _client;
    private readonly Mock<INetworkManager> _network = new();
    private readonly User _user = new("admin", "auth", "reset");

    public PlaybackAccessTests()
    {
        _user.AddDefaultPermissions();
        _user.SetPermission(PermissionKind.IsAdministrator, true);
        _grant = new PlaybackAccessGrant("playback", _user.Id, Guid.NewGuid(), "source", "renderer", "session", DateTimeOffset.UtcNow.AddHours(1));
        var grants = new Mock<IPlaybackAccessManager>();
        grants.Setup(manager => manager.Get("playback")).Returns(_grant);
        var users = new Mock<IUserManager>();
        users.Setup(manager => manager.GetUserById(_user.Id)).Returns(_user);
        _network.Setup(manager => manager.IsInLocalNetwork(It.IsAny<IPAddress>())).Returns(true);
        var auth = new Mock<IAuthService>();
        auth.Setup(service => service.Authenticate(It.IsAny<HttpRequest>()))
            .ReturnsAsync((HttpRequest request) => request.Query["ApiKey"] == "account"
                ? new AuthorizationInfo { User = _user, Token = "account", IsAuthenticated = true }
                : new AuthorizationInfo());

        _server = new HostBuilder().ConfigureWebHost(builder => builder.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAuthorization();
                services.AddSingleton(Mock.Of<IConfigurationManager>());
                services.AddSingleton(Mock.Of<ISyncPlayManager>());
                services.AddHttpContextAccessor();
                services.AddSingleton(grants.Object);
                services.AddSingleton(users.Object);
                services.AddSingleton(_network.Object);
                services.AddSingleton(auth.Object);
                services.AddCustomAuthentication();
                services.AddJellyfinApiAuthorization();
            })
            .Configure(app =>
            {
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapGet("/Videos/{itemId}/master.m3u8", context => context.Response.WriteAsync(
                        context.User.GetMediaAuthorizationQuery() + ";admin=" + context.User.IsInRole(UserRoles.Administrator)))
                        .WithMetadata(new PlaybackAccessAttribute()).RequireAuthorization(Policies.Streaming);
                    endpoints.MapGet("/Videos/{routeItemId}/{routeMediaSourceId}/Subtitles/0/Stream.vtt", context => context.Response.WriteAsync("subtitle"))
                        .WithMetadata(new PlaybackAccessAttribute()).RequireAuthorization(Policies.Streaming);
                    endpoints.MapGet("/Users", context => context.Response.WriteAsync("users")).RequireAuthorization();
                    endpoints.MapGet("/System/Configuration", context => context.Response.WriteAsync("admin")).RequireAuthorization(Policies.RequiresElevation);
                    endpoints.MapPost("/Items/{itemId}/PlaybackAccess", context => context.Response.WriteAsync("created")).RequireAuthorization();
                    endpoints.MapGet("/Other/{itemId}", context => context.Response.WriteAsync("other")).RequireAuthorization(Policies.Streaming);
                });
            })).Build();
        _server.Start();
        _client = _server.GetTestClient();
    }

    [Fact]
    public async Task AdminOwnedGrantPlaysMediaWithoutAnAdministratorRole()
    {
        using var response = await _client.GetAsync(MediaUrl(), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("PlaybackToken=playback&PlaySessionId=session;admin=False", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("/Users")]
    [InlineData("/System/Configuration")]
    public async Task PlaybackCredentialCannotAccessAccountApis(string endpoint)
    {
        using var response = await _client.GetAsync(endpoint + "?PlaybackToken=playback", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PlaybackCredentialCannotIssueAnotherGrant()
    {
        using var response = await _client.PostAsync($"/Items/{_grant.ItemId}/PlaybackAccess?PlaybackToken=playback", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PlaybackCredentialCannotBecomeAnAccountApiKey()
    {
        using var response = await _client.GetAsync("/System/Configuration?ApiKey=playback", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("MediaSourceId", "another-source")]
    [InlineData("DeviceId", "another-device")]
    [InlineData("PlaySessionId", "another-session")]
    [InlineData("PlaybackToken", "invalid")]
    public async Task PlaybackScopeCannotBeChanged(string key, string replacement)
    {
        var url = MediaUrl();
        var original = key switch
        {
            "MediaSourceId" => "source",
            "DeviceId" => "renderer",
            "PlaySessionId" => "session",
            _ => "playback"
        };
        using var response = await _client.GetAsync(url.Replace(key + "=" + original, key + "=" + replacement, StringComparison.Ordinal), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("&MediaSourceId=source")]
    [InlineData("&PlaybackToken=playback")]
    [InlineData("&Params=packed-legacy-parameters")]
    [InlineData("&LiveStreamId=unrelated-live-stream")]
    public async Task AmbiguousAndLegacyParameterOverridesAreRejected(string extraQuery)
    {
        using var response = await _client.GetAsync(MediaUrl() + extraQuery, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AnotherItemCannotBePlayed()
    {
        using var response = await _client.GetAsync(MediaUrl().Replace(_grant.ItemId.ToString(), Guid.NewGuid().ToString(), StringComparison.Ordinal), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task EndpointMustExplicitlyOptIntoPlaybackCredentials()
    {
        using var response = await _client.GetAsync(MediaUrl().Replace("/Videos/", "/Other/", StringComparison.Ordinal).Replace("/master.m3u8", string.Empty, StringComparison.Ordinal), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SubtitleDeliveryUsesTheSamePlaybackScope()
    {
        using var response = await _client.GetAsync($"/Videos/{_grant.ItemId}/source/Subtitles/0/Stream.vtt?PlaybackToken=playback&PlaySessionId=session", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LegacySubtitleQueryCannotOverrideTheAuthorizedItem()
    {
        using var response = await _client.GetAsync($"/Videos/{_grant.ItemId}/source/Subtitles/0/Stream.vtt?PlaybackToken=playback&PlaySessionId=session&itemId={Guid.NewGuid()}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LocalOnlyOwnerCannotDelegateRemotePlayback()
    {
        _user.SetPermission(PermissionKind.EnableRemoteAccess, false);
        _network.Setup(manager => manager.IsInLocalNetwork(It.IsAny<IPAddress>())).Returns(false);
        using var response = await _client.GetAsync(MediaUrl(), TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ExistingAccountAuthenticationStillWorks()
    {
        using var response = await _client.GetAsync("/System/Configuration?ApiKey=account", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _server.Dispose();
    }

    private string MediaUrl()
        => $"/Videos/{_grant.ItemId}/master.m3u8?MediaSourceId=source&DeviceId=renderer&PlaySessionId=session&PlaybackToken=playback";
}

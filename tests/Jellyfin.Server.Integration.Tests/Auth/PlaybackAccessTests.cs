using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
    private readonly Guid _itemId = Guid.NewGuid();
    private readonly User _user = new("admin", "auth", "reset");
    private readonly Mock<INetworkManager> _networkManager = new();
    private readonly IHost _host;
    private readonly HttpClient _client;

    public PlaybackAccessTests()
    {
        _user.AddDefaultPermissions();
        _user.SetPermission(PermissionKind.IsAdministrator, true);
        var grant = new PlaybackAccessGrant("grant", _user.Id, _itemId, "source", "device", "session", DateTime.UtcNow.AddHours(1));
        var playbackAccessManager = new Mock<IPlaybackAccessManager>();
        playbackAccessManager.Setup(i => i.Get("grant")).Returns(grant);
        var userManager = new Mock<IUserManager>();
        userManager.Setup(i => i.GetUserById(_user.Id)).Returns(_user);
        _networkManager.Setup(i => i.IsInLocalNetwork(It.IsAny<IPAddress>())).Returns(true);
        var authService = new Mock<IAuthService>();
        authService.Setup(i => i.Authenticate(It.IsAny<HttpRequest>()))
            .ReturnsAsync((HttpRequest request) => request.Query["ApiKey"] == "account"
                ? new AuthorizationInfo { User = _user, Token = "account", IsAuthenticated = true }
                : new AuthorizationInfo());

        _host = new HostBuilder().ConfigureWebHost(builder => builder.UseTestServer()
            .ConfigureServices(services =>
            {
                services.AddRouting();
                services.AddAuthorization();
                services.AddHttpContextAccessor();
                services.AddSingleton(Mock.Of<IConfigurationManager>());
                services.AddSingleton(Mock.Of<ISyncPlayManager>());
                services.AddSingleton(playbackAccessManager.Object);
                services.AddSingleton(userManager.Object);
                services.AddSingleton(_networkManager.Object);
                services.AddSingleton(authService.Object);
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
                        context.User.GetMediaAuthorizationQuery() + ";" + context.User.IsInRole(UserRoles.Administrator)))
                        .RequireAuthorization().RequireAuthorization(Policies.Streaming);
                    endpoints.MapGet("/Videos/{itemId}/{mediaSourceId}/Subtitles/0/subtitles.m3u8", context => context.Response.WriteAsync("subtitles"))
                        .RequireAuthorization(Policies.Streaming);
                    endpoints.MapGet("/Users", context => context.Response.WriteAsync("users")).RequireAuthorization();
                    endpoints.MapGet("/System/Configuration", context => context.Response.WriteAsync("configuration"))
                        .RequireAuthorization(Policies.RequiresElevation);
                });
            })).Build();
        _host.Start();
        _client = _host.GetTestClient();
    }

    private string StreamUrl => $"/Videos/{_itemId}/master.m3u8?MediaSourceId=source&DeviceId=device&PlaySessionId=session&PlaybackToken=grant";

    [Fact]
    public async Task Stream_Grant_AuthenticatesWithoutAccountRole()
    {
        using var response = await _client.GetAsync(StreamUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("PlaybackToken=grant;False", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Stream_AccessToken_StillAuthenticates()
    {
        using var response = await _client.GetAsync($"/Videos/{_itemId}/master.m3u8?ApiKey=account", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("ApiKey=account;True", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Theory]
    [InlineData("MediaSourceId=source", "MediaSourceId=other")]
    [InlineData("DeviceId=device", "DeviceId=other")]
    [InlineData("PlaySessionId=session", "PlaySessionId=other")]
    [InlineData("PlaybackToken=grant", "PlaybackToken=other")]
    [InlineData("PlaybackToken=grant", "PlaybackToken=grant&MediaSourceId=other")]
    [InlineData("PlaybackToken=grant", "PlaybackToken=grant&Params=other")]
    [InlineData("PlaybackToken=grant", "PlaybackToken=grant&LiveStreamId=other")]
    [InlineData("/master.m3u8", "0/master.m3u8")]
    public async Task Stream_OutsideGrant_ReturnsUnauthorized(string granted, string requested)
    {
        using var response = await _client.GetAsync(StreamUrl.Replace(granted, requested, StringComparison.Ordinal), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("source", HttpStatusCode.OK)]
    [InlineData("other", HttpStatusCode.Unauthorized)]
    public async Task SubtitlePlaylist_Grant_RequiresGrantedMediaSource(string mediaSourceId, HttpStatusCode expected)
    {
        using var response = await _client.GetAsync($"/Videos/{_itemId}/{mediaSourceId}/Subtitles/0/subtitles.m3u8?PlaybackToken=grant", TestContext.Current.CancellationToken);

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData("/Users")]
    [InlineData("/System/Configuration")]
    public async Task OtherEndpoint_Grant_ReturnsUnauthorized(string url)
    {
        using var response = await _client.GetAsync(url + "?PlaybackToken=grant", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Stream_RemoteAccessDisabled_ReturnsForbidden()
    {
        _user.SetPermission(PermissionKind.EnableRemoteAccess, false);
        _networkManager.Setup(i => i.IsInLocalNetwork(It.IsAny<IPAddress>())).Returns(false);

        using var response = await _client.GetAsync(StreamUrl, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    public void Dispose()
    {
        _client.Dispose();
        _host.Dispose();
    }
}

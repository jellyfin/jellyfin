using System;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Helpers;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.MediaInfo;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public sealed class BoundedRemotePlaybackTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RewriteBoundedRemoteSources_UsesAuthenticatedProxyAndKeepsServerSourceUnchanged(bool isRemote)
    {
        var fixture = new Fixture(1);
        var source = CreateRemoteSource();
        source.IsRemote = isRemote;
        var before = JsonSerializer.Serialize(source);
        fixture.Sources.Setup(value => value.GetPlaybackMediaSources(fixture.Movie, fixture.User, true, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { source });
        var info = await fixture.Helper.GetPlaybackInfo(fixture.Movie, fixture.User, fixture.Context.Request);

        fixture.Helper.RewriteBoundedRemoteSources(fixture.Context, fixture.Movie.Id, MediaType.Video, info.PlaySessionId, info.MediaSources);

        var published = Assert.Single(info.MediaSources);
        var proxy = new Uri(published.Path);
        Assert.Equal("public.example", proxy.Host);
        Assert.StartsWith($"/jellyfin/Videos/{fixture.Movie.Id:N}/stream", proxy.AbsolutePath, StringComparison.Ordinal);
        var query = QueryHelpers.ParseQuery(proxy.Query);
        Assert.Equal("true", query["Static"].ToString());
        Assert.Equal(info.PlaySessionId, query["PlaySessionId"].ToString());
        Assert.Equal(source.Id, query["MediaSourceId"].ToString());
        Assert.Equal("actor-token", query["ApiKey"].ToString());
        Assert.False(query.ContainsKey("originSecret"));
        Assert.Null(published.EncoderPath);
        Assert.Null(published.EncoderProtocol);
        Assert.Empty(published.RequiredHttpHeaders);
        Assert.True(published.SupportsDirectPlay);
        Assert.Equal(before, JsonSerializer.Serialize(source));
    }

    [Fact]
    public void RewriteBoundedRemoteSources_UnlimitedPolicyPreservesDirectRemotePlayback()
    {
        var fixture = new Fixture(0);
        var source = CreateRemoteSource();
        var before = JsonSerializer.Serialize(source);

        fixture.Helper.RewriteBoundedRemoteSources(fixture.Context, fixture.Movie.Id, MediaType.Video, "encoding", new[] { source });

        Assert.Equal(before, JsonSerializer.Serialize(source));
    }

    [Theory]
    [InlineData(MediaType.Audio, MediaProtocol.Http)]
    [InlineData(MediaType.Video, MediaProtocol.File)]
    public void RewriteBoundedRemoteSources_DoesNotChangeAudioOrLocalSources(MediaType mediaType, MediaProtocol protocol)
    {
        var fixture = new Fixture(1);
        var source = CreateRemoteSource();
        source.Protocol = protocol;
        if (protocol == MediaProtocol.File)
        {
            source.Path = "/media/movie.mp4";
        }

        var before = JsonSerializer.Serialize(source);

        fixture.Helper.RewriteBoundedRemoteSources(fixture.Context, fixture.Movie.Id, mediaType, "encoding", new[] { source });

        Assert.Equal(before, JsonSerializer.Serialize(source));
    }

    [Fact]
    public void RewriteBoundedRemoteSources_RepeatedResponsePreparationKeepsTheSameProxy()
    {
        var fixture = new Fixture(1);
        var source = CreateRemoteSource();
        fixture.Helper.RewriteBoundedRemoteSources(fixture.Context, fixture.Movie.Id, MediaType.Video, "encoding", new[] { source });
        var first = JsonSerializer.Serialize(source);

        fixture.Helper.RewriteBoundedRemoteSources(fixture.Context, fixture.Movie.Id, MediaType.Video, "encoding", new[] { source });

        Assert.Equal(first, JsonSerializer.Serialize(source));
    }

    [Fact]
    public void RewriteBoundedRemoteSources_ApplicationApiKeyHasNoPerUserLimit()
    {
        var fixture = new Fixture(1);
        Claim[] claims =
        [
            new Claim(InternalClaimTypes.IsApiKey, "true"),
            new Claim(InternalClaimTypes.Token, "application-key")
        ];
        fixture.Context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
        var source = CreateRemoteSource();
        var before = JsonSerializer.Serialize(source);

        fixture.Helper.RewriteBoundedRemoteSources(fixture.Context, fixture.Movie.Id, MediaType.Video, "encoding", new[] { source });

        Assert.Equal(before, JsonSerializer.Serialize(source));
    }

    private static MediaSourceInfo CreateRemoteSource()
        => new()
        {
            Id = "remote-source",
            Path = "https://origin.example/video.mp4?originSecret=upstream-secret",
            Protocol = MediaProtocol.Http,
            EncoderPath = "https://origin.example/private-encoder-input",
            EncoderProtocol = MediaProtocol.Http,
            SupportsDirectPlay = true,
            SupportsDirectStream = true,
            SupportsTranscoding = true,
            RequiredHttpHeaders = new() { ["Authorization"] = "upstream-authorization" }
        };

    private sealed class Fixture
    {
        public Fixture(int limit)
        {
            User = new User("actor", "authentication", "reset") { MaxActiveVideoStreams = limit };
            Movie = new Movie { Id = Guid.NewGuid() };
            Context = new DefaultHttpContext();
            Claim[] claims =
            [
                new Claim(InternalClaimTypes.UserId, User.Id.ToString("N", CultureInfo.InvariantCulture)),
                new Claim(InternalClaimTypes.PlaybackOwnerId, "actor-record"),
                new Claim(InternalClaimTypes.Token, "actor-token")
            ];
            Context.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
            var users = new Mock<IUserManager>();
            users.Setup(value => value.GetUserById(User.Id)).Returns(User);
            var host = new Mock<IServerApplicationHost>();
            host.Setup(value => value.GetSmartApiUrl(It.IsAny<HttpRequest>())).Returns("https://public.example/jellyfin");
            var configuration = new Mock<IServerConfigurationManager>();
            configuration.Setup(value => value.GetConfiguration(It.IsAny<string>())).Returns(new NetworkConfiguration { BaseUrl = "/jellyfin" });
            Helper = new MediaInfoHelper(
                users.Object,
                Mock.Of<ILibraryManager>(),
                Sources.Object,
                Mock.Of<IMediaEncoder>(),
                configuration.Object,
                NullLogger<MediaInfoHelper>.Instance,
                Mock.Of<INetworkManager>(),
                Mock.Of<IDeviceManager>(),
                host.Object);
        }

        public User User { get; }

        public Movie Movie { get; }

        public HttpContext Context { get; }

        public Mock<IMediaSourceManager> Sources { get; } = new();

        public MediaInfoHelper Helper { get; }
    }
}

using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Helpers;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.IO;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Streaming;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public class AudioHelperTests
{
    [Theory]
    [InlineData(null)]
    [InlineData(";;;true")]
    public async Task GetAudioStream_StaticVideoRejectsQuotaBeforeResolvingMediaOrStartingEncoder(string? legacyParams)
    {
        var fixture = new Fixture(new Movie { Id = Guid.NewGuid() });
        var playback = new VideoPlaybackContext(fixture.UserId, "owner", fixture.Item.Id, "player");
        fixture.Coordinator.Setup(value => value.ResolvePlaySession(fixture.UserId, "owner", fixture.Item.Id, "encoding", out playback))
            .Returns(VideoPlaybackStatus.Success);
        IVideoPlaybackRequest? denied = null;
        fixture.Coordinator.Setup(value => value.TryAcquire(playback, "encoding", 1, out denied))
            .Returns(VideoPlaybackStatus.LimitReached);

        await Assert.ThrowsAsync<VideoPlaybackLimitException>(() => fixture.Helper.GetAudioStream(
            TranscodingJobType.Progressive,
            new StreamingRequestDto { Id = fixture.Item.Id, Static = legacyParams is null, Params = legacyParams, PlaySessionId = "encoding" }));

        Assert.Empty(fixture.Sources.Invocations);
        Assert.Empty(fixture.Transcodes.Invocations);
    }

    [Fact]
    public async Task GetAudioStream_NormalizesPackedAliasBeforeAcquiringAnyPlayback()
    {
        var fixture = new Fixture(new Movie { Id = Guid.NewGuid() });
        var borrowed = default(VideoPlaybackContext);
        fixture.Coordinator.Setup(value => value.ResolvePlaySession(fixture.UserId, "owner", fixture.Item.Id, "borrowed", out borrowed))
            .Returns(VideoPlaybackStatus.OwnershipMismatch);

        await Assert.ThrowsAsync<SecurityException>(() => fixture.Helper.GetAudioStream(
            TranscodingJobType.Progressive,
            new StreamingRequestDto { Id = fixture.Item.Id, Static = true, Params = new string(';', 20) + "borrowed", PlaySessionId = "encoding" }));

        fixture.Coordinator.Verify(value => value.ResolvePlaySession(fixture.UserId, "owner", fixture.Item.Id, "encoding", out It.Ref<VideoPlaybackContext>.IsAny), Times.Never);
        fixture.Coordinator.Verify(value => value.TryAcquire(It.IsAny<VideoPlaybackContext>(), It.IsAny<string>(), It.IsAny<int>(), out It.Ref<IVideoPlaybackRequest?>.IsAny), Times.Never);
        Assert.Empty(fixture.Sources.Invocations);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAudioStream_AudioOnlyOutputDoesNotAcquireVideoQuota(bool staticAudio)
    {
        BaseItem item = staticAudio ? new Audio { Id = Guid.NewGuid() } : new Movie { Id = Guid.NewGuid() };
        var fixture = new Fixture(item);
        var sourceRequested = new InvalidOperationException("Source resolution reached.");
        fixture.Sources.Setup(value => value.GetPlaybackMediaSources(item, null, false, false, It.IsAny<CancellationToken>()))
            .ThrowsAsync(sourceRequested);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Helper.GetAudioStream(
            TranscodingJobType.Progressive,
            new StreamingRequestDto { Id = item.Id, Static = staticAudio, AudioCodec = "aac" }));

        Assert.Same(sourceRequested, exception);
        Assert.Empty(fixture.Coordinator.Invocations);
    }

    private sealed class Fixture
    {
        public Fixture(BaseItem item)
        {
            Item = item;
            var user = new User("player", "authentication", "reset") { Id = UserId, MaxActiveVideoStreams = 1 };
            var users = new Mock<IUserManager>();
            users.Setup(value => value.GetUserById(UserId)).Returns(user);
            var library = new Mock<ILibraryManager>();
            library.Setup(value => value.GetItemById<BaseItem>(item.Id)).Returns(item);
            library.Setup(value => value.GetItemById<BaseItem>(item.Id, UserId)).Returns(item);
            var context = new DefaultHttpContext();
            context.Request.Method = HttpMethods.Get;
            context.Request.Path = $"/Audio/{item.Id}/stream";
            context.User = new ClaimsPrincipal(new ClaimsIdentity(
                new[]
                {
                    new Claim(InternalClaimTypes.UserId, UserId.ToString()),
                    new Claim(InternalClaimTypes.PlaybackOwnerId, "owner")
                },
                "test"));
            var encoder = Mock.Of<IMediaEncoder>();
            var encodingHelper = new EncodingHelper(
                Mock.Of<IApplicationPaths>(),
                encoder,
                Mock.Of<ISubtitleEncoder>(),
                Mock.Of<IConfiguration>(),
                Mock.Of<MediaBrowser.Common.Configuration.IConfigurationManager>(),
                Mock.Of<IPathManager>());
            Helper = new AudioHelper(
                users.Object,
                library.Object,
                Sources.Object,
                Mock.Of<IServerConfigurationManager>(),
                encoder,
                Transcodes.Object,
                Mock.Of<IHttpClientFactory>(),
                new HttpContextAccessor { HttpContext = context },
                encodingHelper,
                new VideoPlaybackHelper(Coordinator.Object, users.Object, library.Object));
        }

        public Guid UserId { get; } = Guid.NewGuid();

        public BaseItem Item { get; }

        public Mock<IMediaSourceManager> Sources { get; } = new();

        public Mock<ITranscodeManager> Transcodes { get; } = new();

        public Mock<IVideoPlaybackCoordinator> Coordinator { get; } = new();

        public AudioHelper Helper { get; }
    }
}

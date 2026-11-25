using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Emby.Server.Implementations.Session;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SessionManager;

public sealed class LiveStreamPlaybackManagerTests
{
    [Fact]
    public void Verify_SharedInputRemainsBoundToUserTokenItemAndLogicalPlayer()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "input");
        var otherPlayer = owner with { PlaybackInstanceId = "other-player" };
        Assert.Equal(VideoPlaybackStatus.Success, fixture.Coordinator.RegisterPlaySession(otherPlayer, "other-player-encoding"));

        Assert.True(fixture.Manager.Verify(owner.UserId, owner.SessionId, owner.ItemId, "shared", "INPUT"));
        Assert.False(fixture.Manager.Verify(Guid.NewGuid(), owner.SessionId, owner.ItemId, "shared", "input"));
        Assert.False(fixture.Manager.Verify(owner.UserId, "another-token", owner.ItemId, "shared", "input"));
        Assert.False(fixture.Manager.Verify(owner.UserId, owner.SessionId, Guid.NewGuid(), "shared", "input"));
        Assert.False(fixture.Manager.Verify(owner.UserId, owner.SessionId, owner.ItemId, "shared", "other-player-encoding"));
    }

    [Fact]
    public void Verify_QualityChangeCanReuseItsExistingInputConsumer()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "input");
        Assert.Equal(VideoPlaybackStatus.Success, fixture.Coordinator.RegisterPlaySession(owner, "new-encoding"));

        Assert.True(fixture.Manager.Verify(owner.UserId, owner.SessionId, owner.ItemId, "shared", "new-encoding"));
        Assert.False(fixture.Manager.ContainsPlaySession("new-encoding"));
        Assert.True(fixture.Manager.ContainsPlaySession("INPUT"));
    }

    [Fact]
    public void Register_DuplicateIsIdempotentAndConflictingOwnerCannotAdoptAlias()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "input");

        Assert.False(fixture.Manager.Register(owner, "INPUT", "SHARED", "session"));
        Assert.Throws<SecurityException>(() => fixture.Manager.Register(owner with { SessionId = "another-token" }, "INPUT", "shared", "session"));
        Assert.Single(fixture.Manager.GetProtectedPlaySessions());
        Assert.Empty(fixture.Closed);
    }

    [Fact]
    public void Register_SameAuthenticatedOwnerWithDifferentClientHeadersIsStillIdempotent()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "input", "casting-session");

        Assert.False(fixture.Manager.Register(owner, "INPUT", "shared", "native-fetch-session"));
        Assert.Single(fixture.Manager.GetProtectedPlaySessions());
        Assert.True(fixture.Manager.Verify(owner.UserId, owner.SessionId, owner.ItemId, "shared", "input"));
    }

    [Fact]
    public async Task VerifyOwner_ControlRequiresTheAuthenticatedTokenEvenWhenClientSessionMatches()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "input", "shared-client-session");

        Assert.True(fixture.Manager.VerifyOwner(owner.UserId, owner.SessionId, "shared", out var hasOpenConsumer));
        Assert.True(hasOpenConsumer);
        Assert.False(fixture.Manager.VerifyOwner(owner.UserId, "copied-header-token", "shared", out _));
        Assert.False(fixture.Manager.VerifyOwner(Guid.NewGuid(), owner.SessionId, "shared", out _));
        Assert.False(fixture.Manager.Verify(owner.UserId, owner.SessionId, Guid.Empty, "shared", "input"));
        await fixture.Manager.CloseOwnedAsync(owner.UserId, owner.SessionId, "shared", "input");
        fixture.ResourceExists = false;
        await fixture.Manager.RemoveExpiredAsync();
        Assert.True(fixture.Manager.VerifyOwner(owner.UserId, owner.SessionId, "shared", out hasOpenConsumer));
        Assert.False(hasOpenConsumer);
        Assert.False(fixture.Manager.VerifyOwner(owner.UserId, "copied-header-token", "shared", out _));
    }

    [Fact]
    public void TryGetLiveStream_ExistingOpenCannotBeBorrowedOrReusedAfterResourceCloses()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "input");

        Assert.True(fixture.Manager.TryGetLiveStream(owner.UserId, owner.SessionId, owner.ItemId, "INPUT", out var liveStreamId));
        Assert.Equal("shared", liveStreamId);
        Assert.False(fixture.Manager.TryGetLiveStream(owner.UserId, "another-token", owner.ItemId, "input", out _));
        fixture.ResourceExists = false;
        Assert.False(fixture.Manager.TryGetLiveStream(owner.UserId, owner.SessionId, owner.ItemId, "input", out _));
    }

    [Fact]
    public async Task ClosePlayback_ReleasesItsConsumersOnceAndPreservesAnotherOwner()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        var other = owner with { SessionId = "another-token", PlaybackInstanceId = "other-player" };
        fixture.Register(owner, "old-input");
        fixture.Register(owner, "new-input");
        fixture.Register(other, "other-input");
        Assert.Equal(VideoPlaybackStatus.Success, fixture.Coordinator.Stop(owner, "new-input"));

        Assert.True(await fixture.Manager.ClosePlaybackAsync("shared", owner));
        Assert.Equal(2, fixture.Closed.Count);
        Assert.True(await fixture.Manager.ClosePlaybackAsync("shared", owner));
        Assert.True(await fixture.Manager.CloseSessionAsync("shared", "missing-session"));
        Assert.Equal(2, fixture.Closed.Count);
        Assert.True(fixture.Manager.Verify(other.UserId, other.SessionId, other.ItemId, "shared", "other-input"));
        Assert.Single(fixture.Manager.GetProtectedPlaySessions());
    }

    [Fact]
    public async Task CloseSession_TransportDisconnectDoesNotCloseAnAuthorizedVideo()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "input");

        Assert.True(await fixture.Manager.CloseSessionAsync("shared", "session"));
        Assert.Empty(fixture.Closed);
        Assert.Equal(VideoPlaybackStatus.Success, fixture.Coordinator.Stop(owner, "input"));
        Assert.True(await fixture.Manager.CloseSessionAsync("shared", "session"));
        Assert.Single(fixture.Closed);
        Assert.True(await fixture.Manager.CloseSessionAsync("shared", "session"));
        Assert.Single(fixture.Closed);
        Assert.False(await fixture.Manager.CloseSessionAsync("unknown-resource", "session"));
    }

    [Fact]
    public async Task CloseOwned_RepeatedCloseIsIdempotentAndAnotherTokenCannotCloseTheInput()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "input");

        await Assert.ThrowsAsync<SecurityException>(() => fixture.Manager.CloseOwnedAsync(owner.UserId, "another-token", "shared", "input"));
        await fixture.Manager.CloseOwnedAsync(owner.UserId, owner.SessionId, "shared", "INPUT");
        await fixture.Manager.CloseOwnedAsync(owner.UserId, owner.SessionId, "shared", "input");
        Assert.Single(fixture.Closed);
    }

    [Fact]
    public async Task CloseOwned_LegacyCloseDoesNotGuessBetweenIndependentPlayers()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        var second = owner with { PlaybackInstanceId = "second-player" };
        fixture.Register(owner, "first-input");
        fixture.Register(second, "second-input");

        await fixture.Manager.CloseOwnedAsync(owner.UserId, owner.SessionId, "shared");
        Assert.Empty(fixture.Closed);
        await fixture.Manager.CloseOwnedAsync(owner.UserId, owner.SessionId, "shared", "first-input");
        Assert.Single(fixture.Closed);
        Assert.True(fixture.Manager.Verify(second.UserId, second.SessionId, second.ItemId, "shared", "second-input"));
    }

    [Fact]
    public async Task CloseOwned_LegacyCloseDoesNotInterruptAReplacementEncoding()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "input");
        Assert.Equal(VideoPlaybackStatus.Success, fixture.Coordinator.RegisterPlaySession(owner, "replacement"));
        Assert.Equal(VideoPlaybackStatus.Success, fixture.Coordinator.TryAcquire(owner, "replacement", 1, out var operation));
        using (var request = Assert.IsAssignableFrom<IVideoPlaybackRequest>(operation))
        {
            request.Acknowledge();
        }

        await fixture.Manager.CloseOwnedAsync(owner.UserId, owner.SessionId, "shared");
        Assert.Empty(fixture.Closed);
        await fixture.Manager.CloseOwnedAsync(owner.UserId, owner.SessionId, "shared", "replacement");
        Assert.Single(fixture.Closed);
    }

    [Fact]
    public async Task RemoveExpired_ReleasesAbandonedVideoButPreservesAudio()
    {
        var fixture = new Fixture();
        var video = fixture.NewPlayback();
        var audio = fixture.NewPlayback();
        fixture.Register(video, "video");
        fixture.Register(audio, "audio", isVideo: false);
        fixture.Clock.Advance(TimeSpan.FromMinutes(6));

        await fixture.Manager.RemoveExpiredAsync();

        Assert.Single(fixture.Closed);
        Assert.False(fixture.Manager.ContainsPlaySession("video"));
        Assert.True(fixture.Manager.Verify(audio.UserId, audio.SessionId, audio.ItemId, "shared", "different-audio-encoding"));
    }

    [Fact]
    public async Task RemoveExpired_RemovesClosedResourceWithoutDecrementingAnotherConsumer()
    {
        var fixture = new Fixture();
        fixture.Register(fixture.NewPlayback(), "input");
        fixture.ResourceExists = false;

        await fixture.Manager.RemoveExpiredAsync();

        Assert.Empty(fixture.Manager.GetProtectedPlaySessions());
        Assert.Empty(fixture.Closed);
        Assert.True(await fixture.Manager.CloseSessionAsync("shared", "session"));
        fixture.Clock.Advance(TimeSpan.FromMinutes(6));
        await fixture.Manager.RemoveExpiredAsync();
        Assert.False(await fixture.Manager.CloseSessionAsync("shared", "session"));
    }

    [Fact]
    public async Task RemoveExpired_DoesNotForgetAnInputReopenedDuringTheResourceLookup()
    {
        var fixture = new Fixture();
        var previous = fixture.NewPlayback();
        var replacement = fixture.NewPlayback();
        fixture.Register(previous, "previous-input");
        var lookedUp = false;
        fixture.Sources.Setup(value => value.GetLiveStreamInfo("shared"))
            .Returns(() =>
            {
                if (!lookedUp)
                {
                    lookedUp = true;
                    fixture.Register(replacement, "reopened-input");
                    return null!;
                }

                return Mock.Of<ILiveStream>();
            });

        await fixture.Manager.RemoveExpiredAsync();

        Assert.False(fixture.Manager.ContainsPlaySession("previous-input"));
        Assert.True(fixture.Manager.ContainsPlaySession("reopened-input"));
        Assert.True(fixture.Manager.Verify(replacement.UserId, replacement.SessionId, replacement.ItemId, "shared", "reopened-input"));
        Assert.Empty(fixture.Closed);
    }

    [Fact]
    public async Task ConcurrentClose_ReleasesOneProviderReference()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "input");
        Assert.Equal(VideoPlaybackStatus.Success, fixture.Coordinator.Stop(owner, "input"));

        await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() => fixture.Manager.ClosePlaybackAsync("shared", owner))));

        Assert.Single(fixture.Closed);
        Assert.Empty(fixture.Manager.GetProtectedPlaySessions());
    }

    [Fact]
    public async Task CloseFailure_IsRetriedByCleanupWithoutRestoringMediaAccess()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "input");
        Assert.Equal(VideoPlaybackStatus.Success, fixture.Coordinator.Stop(owner, "input"));
        fixture.Sources.SetupSequence(value => value.CloseLiveStream("shared"))
            .ThrowsAsync(new IOException("temporary close failure"))
            .Returns(Task.CompletedTask);

        await Assert.ThrowsAsync<IOException>(() => fixture.Manager.ClosePlaybackAsync("shared", owner));
        Assert.False(fixture.Manager.Verify(owner.UserId, owner.SessionId, owner.ItemId, "shared", "input"));
        await fixture.Manager.RemoveExpiredAsync();

        fixture.Sources.Verify(value => value.CloseLiveStream("shared"), Times.Exactly(2));
    }

    [Fact]
    public async Task ClosedOwnerRetention_ExpiresWithoutForgettingTheManagedResource()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "input");
        await fixture.Manager.CloseOwnedAsync(owner.UserId, owner.SessionId, "shared", "input");
        fixture.Clock.Advance(TimeSpan.FromMinutes(6));
        await fixture.Manager.RemoveExpiredAsync();

        await Assert.ThrowsAsync<SecurityException>(() => fixture.Manager.CloseOwnedAsync(owner.UserId, owner.SessionId, "shared", "input"));
        Assert.True(await fixture.Manager.CloseSessionAsync("shared", "missing-session"));
        Assert.Single(fixture.Closed);
    }

    [Fact]
    public async Task ClosePlayback_LateCleanupDoesNotCloseARestartWithTheSameLegacyContext()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "old-input");
        Assert.Equal(VideoPlaybackStatus.Success, fixture.Coordinator.Stop(owner, "old-input"));
        fixture.Register(owner, "new-input");

        Assert.True(await fixture.Manager.ClosePlaybackAsync("shared", owner));

        Assert.Single(fixture.Closed);
        Assert.True(fixture.Manager.Verify(owner.UserId, owner.SessionId, owner.ItemId, "shared", "new-input"));
        Assert.True(fixture.Manager.ContainsPlaySession("new-input"));
    }

    [Fact]
    public async Task CloseOwned_ReplayedOldAliasDoesNotCloseAReplacementConsumer()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "old-input");
        await fixture.Manager.CloseOwnedAsync(owner.UserId, owner.SessionId, "shared", "old-input");
        fixture.Register(owner, "new-input");

        await fixture.Manager.CloseOwnedAsync(owner.UserId, owner.SessionId, "shared", "OLD-INPUT");

        Assert.Single(fixture.Closed);
        Assert.True(fixture.Manager.Verify(owner.UserId, owner.SessionId, owner.ItemId, "shared", "new-input"));
    }

    [Fact]
    public async Task CloseSession_OldEncodingCannotReleaseTheInputReusedByItsReplacement()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "old-input");
        Assert.Equal(VideoPlaybackStatus.Success, fixture.Coordinator.RegisterPlaySession(owner, "replacement"));
        Assert.Equal(VideoPlaybackStatus.Success, fixture.Coordinator.TryAcquire(owner, "replacement", 1, out var operation));
        using var request = Assert.IsAssignableFrom<IVideoPlaybackRequest>(operation);

        Assert.True(await fixture.Manager.CloseSessionAsync("shared", "old-input"));
        Assert.Empty(fixture.Closed);
        Assert.True(fixture.Manager.Verify(owner.UserId, owner.SessionId, owner.ItemId, "shared", "replacement"));

        fixture.Register(owner, "replacement");
        Assert.True(await fixture.Manager.CloseSessionAsync("shared", "old-input"));
        Assert.Single(fixture.Closed);
        Assert.True(fixture.Manager.Verify(owner.UserId, owner.SessionId, owner.ItemId, "shared", "replacement"));
    }

    [Fact]
    public void Verify_NewLegacyEpochCannotAdoptAnOldTerminalVideoInput()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "old-input");
        Assert.Equal(VideoPlaybackStatus.Success, fixture.Coordinator.Stop(owner, "old-input"));
        Assert.Equal(VideoPlaybackStatus.Success, fixture.Coordinator.RegisterPlaySession(owner, "new-epoch"));

        Assert.False(fixture.Manager.Verify(owner.UserId, owner.SessionId, owner.ItemId, "shared", "new-epoch"));
        Assert.True(fixture.Manager.Verify(owner.UserId, owner.SessionId, owner.ItemId, "shared", "new-epoch", isVideoOutput: false));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task RemoveExpired_ActiveAudioOutputKeepsItsInputAfterVideoMetadataExpires(int activeRequests)
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "input");
        fixture.Clock.Advance(TimeSpan.FromMinutes(6));
        fixture.Coordinator.RemoveExpired();
        using var job = CreateAudioJob(owner, activeRequests);

        await fixture.Manager.RemoveExpiredAsync(new[] { job });

        Assert.Empty(fixture.Closed);
        Assert.True(fixture.Manager.Verify(owner.UserId, owner.SessionId, owner.ItemId, "shared", "input", isVideoOutput: false));
        Assert.False(fixture.Manager.Verify(owner.UserId, owner.SessionId, owner.ItemId, "shared", "input"));
    }

    [Fact]
    public async Task RemoveExpired_AudioJobOfAnotherTokenDoesNotKeepAVideoInputAlive()
    {
        var fixture = new Fixture();
        var owner = fixture.NewPlayback();
        fixture.Register(owner, "input");
        fixture.Clock.Advance(TimeSpan.FromMinutes(6));
        using var job = CreateAudioJob(owner with { SessionId = "another-token" }, 1);

        await fixture.Manager.RemoveExpiredAsync(new[] { job });

        Assert.Single(fixture.Closed);
    }

    private static TranscodingJob CreateAudioJob(VideoPlaybackContext owner, int activeRequests)
        => new(NullLogger<TranscodingJob>.Instance)
        {
            UserId = owner.UserId,
            ItemId = owner.ItemId,
            PlaybackOwnerId = owner.SessionId,
            LiveStreamId = "SHARED",
            IsOutputVideo = false,
            ActiveRequestCount = activeRequests
        };

    private sealed class Fixture
    {
        private readonly ILiveStream _stream = Mock.Of<ILiveStream>();

        public Fixture()
        {
            Sources.Setup(value => value.GetLiveStreamInfo(It.IsAny<string>())).Returns(() => ResourceExists ? _stream : null!);
            Sources.Setup(value => value.CloseLiveStream(It.IsAny<string>())).Returns((string id) =>
            {
                Closed.Add(id);
                return Task.CompletedTask;
            });
            Coordinator = new VideoPlaybackCoordinator(Clock, TimeSpan.FromMinutes(5));
            Manager = new LiveStreamPlaybackManager(Coordinator, Sources.Object, Clock);
        }

        public ManualTimeProvider Clock { get; } = new();

        public Mock<IMediaSourceManager> Sources { get; } = new();

        public VideoPlaybackCoordinator Coordinator { get; }

        public LiveStreamPlaybackManager Manager { get; }

        public ConcurrentBag<string> Closed { get; } = [];

        public bool ResourceExists { get; set; } = true;

        public VideoPlaybackContext NewPlayback() => new(Guid.NewGuid(), "authenticated-owner", Guid.NewGuid(), Guid.NewGuid().ToString("N"));

        public void Register(VideoPlaybackContext playback, string playSessionId, string sessionId = "session", bool isVideo = true)
        {
            if (isVideo)
            {
                Assert.Equal(VideoPlaybackStatus.Success, Coordinator.RegisterPlaySession(playback, playSessionId));
            }

            Assert.True(Manager.Register(playback, playSessionId, "shared", sessionId, isVideo));
        }
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _timestamp;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => _timestamp;

        public void Advance(TimeSpan elapsed) => _timestamp += elapsed.Ticks;
    }
}

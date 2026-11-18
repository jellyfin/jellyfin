using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Emby.Server.Implementations.Session;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Helpers;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Session;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Helpers;

public sealed class VideoPlaybackHelperTests
{
    [Fact]
    public void RegisterPlayback_MetadataForMultiplePlayers_DoesNotReserveQuota()
    {
        var fixture = new PlaybackFixture();
        var context = fixture.CreateContext("42");

        Assert.Equal("player-a", fixture.Helper.RegisterPlayback(context, fixture.Movie.Id, "encoding-a", "player-a"));
        Assert.Equal("player-b", fixture.Helper.RegisterPlayback(context, fixture.Movie.Id, "encoding-b", "player-b"));
        Assert.Equal("player-c", fixture.Helper.RegisterPlayback(context, fixture.Movie.Id, "encoding-c", "player-c"));

        using var request = fixture.Helper.Acquire(context, fixture.Movie.Id, "encoding-c");
        Assert.NotNull(request);
    }

    [Fact]
    public async Task Acquire_ParallelRequestsForReplacementEncodings_ReuseOneAdmission()
    {
        var fixture = new PlaybackFixture();
        var first = fixture.CreateContext("42");
        var second = fixture.CreateContext("43");
        fixture.Helper.RegisterPlayback(first, fixture.Movie.Id, "original-encoding", "player-a");
        fixture.Helper.RegisterPlayback(first, fixture.Movie.Id, "replacement-encoding", "player-a");
        fixture.Helper.RegisterPlayback(second, fixture.Movie.Id, "second-encoding", "player-b");
        var requests = await Task.WhenAll(Enumerable.Range(0, 16).Select(index => Task.Run(() =>
            fixture.Helper.Acquire(first, fixture.Movie.Id, (index % 2) == 0 ? "original-encoding" : "replacement-encoding")))).ConfigureAwait(true);
        try
        {
            Assert.All(requests, request => Assert.NotNull(request));
            Assert.Throws<VideoPlaybackLimitException>(() => fixture.Helper.Acquire(second, fixture.Movie.Id, "second-encoding"));
            foreach (var request in requests)
            {
                request!.Acknowledge();
            }
        }
        finally
        {
            foreach (var request in requests)
            {
                request?.Dispose();
            }
        }

        using var seek = fixture.Helper.Acquire(first, fixture.Movie.Id, "replacement-encoding");
        Assert.NotNull(seek);
        Assert.Throws<VideoPlaybackLimitException>(() => fixture.Helper.Acquire(second, fixture.Movie.Id, "second-encoding"));
    }

    [Fact]
    public async Task Wrap_BodyExecuting_BlocksSecondOwnerAndAllowsFirstOwnerToContinue()
    {
        var fixture = new PlaybackFixture();
        var first = fixture.CreateContext("42");
        var second = fixture.CreateContext("43");
        fixture.Helper.RegisterPlayback(first, fixture.Movie.Id, "first-encoding", "player-a");
        fixture.Helper.RegisterPlayback(second, fixture.Movie.Id, "second-encoding", "player-b");
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var complete = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var inner = new DelegateResult(async _ =>
        {
            entered.SetResult();
            await complete.Task.ConfigureAwait(true);
        });
        var operation = fixture.Helper.Acquire(first, fixture.Movie.Id, "first-encoding");
        var result = VideoPlaybackHelper.Wrap(inner, operation);
        operation!.Dispose();

        var execution = result.ExecuteResultAsync(new ActionContext { HttpContext = first });
        await entered.Task.ConfigureAwait(true);
        try
        {
            Assert.Throws<VideoPlaybackLimitException>(() => fixture.Helper.Acquire(second, fixture.Movie.Id, "second-encoding"));
            first.Request.Headers.Range = "bytes=1024-2047";
            using var seek = fixture.Helper.Acquire(first, fixture.Movie.Id, "first-encoding");
            Assert.NotNull(seek);
            seek.Acknowledge();
            Assert.False(execution.IsCompleted);
        }
        finally
        {
            complete.SetResult();
        }

        await execution.ConfigureAwait(true);
        Assert.Throws<VideoPlaybackLimitException>(() => fixture.Helper.Acquire(second, fixture.Movie.Id, "second-encoding"));
    }

    [Fact]
    public void Acquire_HeadAudioAndApplicationKey_DoNotReserveQuota()
    {
        var fixture = new PlaybackFixture();
        var head = fixture.CreateContext("42");
        head.Request.Method = HttpMethods.Head;
        Assert.Null(fixture.Helper.Acquire(head, fixture.Movie.Id, null));
        Assert.Null(fixture.Helper.Acquire(fixture.CreateContext("42"), fixture.Audio.Id, null));
        Assert.Null(fixture.Helper.Acquire(new DefaultHttpContext(), fixture.Movie.Id, null));

        using var request = fixture.Helper.Acquire(fixture.CreateContext("43"), fixture.Movie.Id, null);
        Assert.NotNull(request);
    }

    [Fact]
    public void Acquire_AliasUsedByAnotherVerifiedOwner_RejectsWithoutAffectingOriginal()
    {
        var fixture = new PlaybackFixture();
        var first = fixture.CreateContext("42");
        var impersonator = fixture.CreateContext("43");
        fixture.Helper.RegisterPlayback(first, fixture.Movie.Id, "first-encoding", "player-a");

        // Both principals advertise the same client DeviceId. Their verified token owners differ.
        Assert.Throws<SecurityException>(() => fixture.Helper.Acquire(impersonator, fixture.Movie.Id, "first-encoding"));

        using var request = fixture.Helper.Acquire(first, fixture.Movie.Id, "first-encoding");
        Assert.NotNull(request);
    }

    [Fact]
    public void Acquire_RegisteredAliasUsedForAnotherItem_IsRejected()
    {
        var fixture = new PlaybackFixture();
        var context = fixture.CreateContext("42");
        fixture.Helper.RegisterPlayback(context, fixture.Movie.Id, "first-encoding", "player-a");

        Assert.Throws<SecurityException>(() => fixture.Helper.Acquire(context, fixture.OtherMovie.Id, "first-encoding"));

        using var request = fixture.Helper.Acquire(context, fixture.Movie.Id, "first-encoding");
        Assert.NotNull(request);
    }

    [Fact]
    public void Acquire_LegacyPlayerRestartsAfterStop_CreatesFreshAliasImmediately()
    {
        var fixture = new PlaybackFixture();
        var context = fixture.CreateContext("42");
        using (var first = fixture.Helper.Acquire(context, fixture.Movie.Id, null))
        {
            Assert.NotNull(first);
            first.Acknowledge();
        }

        var legacy = new VideoPlaybackContext(
            fixture.User.Id,
            "42",
            fixture.Movie.Id,
            VideoPlaybackContext.CreateLegacyId(fixture.User.Id, "42", fixture.Movie.Id));
        Assert.Equal(VideoPlaybackStatus.Success, fixture.Coordinator.ResolvePlayback(legacy, out var firstAlias));
        Assert.NotNull(firstAlias);
        Assert.Equal(VideoPlaybackStatus.Success, fixture.Coordinator.Stop(legacy, firstAlias));

        using var restarted = fixture.Helper.Acquire(context, fixture.Movie.Id, null);
        Assert.NotNull(restarted);
        Assert.Equal(VideoPlaybackStatus.Success, fixture.Coordinator.ResolvePlayback(legacy, out var restartedAlias));
        Assert.NotEqual(firstAlias, restartedAlias);
        Assert.Equal(VideoPlaybackStatus.Stopped, fixture.Coordinator.ResolvePlaySession(fixture.User.Id, "42", fixture.Movie.Id, firstAlias, out _));
    }

    [Fact]
    public void Acquire_LimitReduced_ExistingPlayersContinueAndNewPlayerIsDenied()
    {
        var fixture = new PlaybackFixture();
        fixture.User.MaxActiveVideoStreams = 2;
        var context = fixture.CreateContext("42");
        fixture.Helper.RegisterPlayback(context, fixture.Movie.Id, "encoding-a", "player-a");
        fixture.Helper.RegisterPlayback(context, fixture.Movie.Id, "encoding-b", "player-b");
        fixture.Helper.RegisterPlayback(context, fixture.Movie.Id, "encoding-c", "player-c");
        using (var first = fixture.Helper.Acquire(context, fixture.Movie.Id, "encoding-a"))
        using (var second = fixture.Helper.Acquire(context, fixture.Movie.Id, "encoding-b"))
        {
            first!.Acknowledge();
            second!.Acknowledge();
        }

        fixture.User.MaxActiveVideoStreams = 1;
        using var continueFirst = fixture.Helper.Acquire(context, fixture.Movie.Id, "encoding-a");
        using var continueSecond = fixture.Helper.Acquire(context, fixture.Movie.Id, "encoding-b");
        Assert.NotNull(continueFirst);
        Assert.NotNull(continueSecond);
        Assert.Throws<VideoPlaybackLimitException>(() => fixture.Helper.Acquire(context, fixture.Movie.Id, "encoding-c"));
    }

    private sealed class PlaybackFixture
    {
        public PlaybackFixture()
        {
            User = new User("playback-test", "test-authentication", "test-password-reset") { MaxActiveVideoStreams = 1 };
            Movie = new Movie { Id = Guid.NewGuid() };
            OtherMovie = new Movie { Id = Guid.NewGuid() };
            Audio = new Audio { Id = Guid.NewGuid() };
            var users = new Mock<IUserManager>(MockBehavior.Strict);
            users.Setup(value => value.GetUserById(User.Id)).Returns(User);
            var library = new Mock<ILibraryManager>(MockBehavior.Strict);
            library.Setup(value => value.GetItemById<BaseItem>(Movie.Id, User.Id)).Returns(Movie);
            library.Setup(value => value.GetItemById<BaseItem>(OtherMovie.Id, User.Id)).Returns(OtherMovie);
            library.Setup(value => value.GetItemById<BaseItem>(Audio.Id, User.Id)).Returns(Audio);
            Coordinator = new VideoPlaybackCoordinator(new FixedTimeProvider(), TimeSpan.FromMinutes(2));
            Helper = new VideoPlaybackHelper(Coordinator, users.Object, library.Object);
        }

        public User User { get; }

        public Movie Movie { get; }

        public Movie OtherMovie { get; }

        public Audio Audio { get; }

        public VideoPlaybackCoordinator Coordinator { get; }

        public VideoPlaybackHelper Helper { get; }

        public HttpContext CreateContext(string ownerId)
            => new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(InternalClaimTypes.UserId, User.Id.ToString("N")),
                        new Claim(InternalClaimTypes.PlaybackOwnerId, ownerId),
                        new Claim(InternalClaimTypes.DeviceId, "same-client-supplied-device-id")
                    ],
                    "PlaybackTest"))
            };
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override long GetTimestamp() => 0;
    }

    private sealed class DelegateResult(Func<ActionContext, Task> execute) : ActionResult
    {
        public override Task ExecuteResultAsync(ActionContext context) => execute(context);
    }
}

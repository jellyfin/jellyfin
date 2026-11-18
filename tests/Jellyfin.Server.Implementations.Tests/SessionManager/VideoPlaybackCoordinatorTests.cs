using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Session;
using MediaBrowser.Controller.Session;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.SessionManager;

public class VideoPlaybackCoordinatorTests
{
    private static readonly TimeSpan _timeout = TimeSpan.FromMinutes(2);

    [Theory]
    [InlineData(0, 12)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    public async Task TryAcquire_SimultaneousDistinctPlaybacks_RespectsUserLimit(int limit, int expectedAdmissions)
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        var playbacks = Enumerable.Range(0, 12)
            .Select(index => owner with { PlaybackInstanceId = $"playback-{index}" })
            .ToArray();
        for (var index = 0; index < playbacks.Length; index++)
        {
            Register(coordinator, playbacks[index], $"encoding-{index}");
        }

        var results = await RunTogether(playbacks.Length, index =>
        {
            var status = coordinator.TryAcquire(playbacks[index], $"encoding-{index}", limit, out var request);
            return (Status: status, Request: request);
        });

        Assert.Equal(expectedAdmissions, results.Count(result => result.Status == VideoPlaybackStatus.Success));
        Assert.All(results.Where(result => result.Status != VideoPlaybackStatus.Success), result =>
        {
            Assert.Equal(VideoPlaybackStatus.LimitReached, result.Status);
            Assert.Null(result.Request);
        });
        foreach (var result in results)
        {
            result.Request?.Dispose();
        }
    }

    [Fact]
    public void RegisterPlaySession_DoesNotConsumeQuota()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        for (var index = 0; index < 20; index++)
        {
            Register(coordinator, owner with { PlaybackInstanceId = $"idle-{index}" }, $"idle-encoding-{index}");
        }

        var active = owner with { PlaybackInstanceId = "active" };
        Register(coordinator, active, "active-encoding");
        using var request = Acquire(coordinator, active, "active-encoding", 1);
    }

    [Fact]
    public void RemoveExpired_RetiresObsoleteAliasesWhileKeepingCurrentAndProtectedAliases()
    {
        var clock = new ManualTimeProvider();
        var coordinator = new VideoPlaybackCoordinator(clock, _timeout);
        var owner = NewPlayback();
        Register(coordinator, owner, "obsolete");
        Register(coordinator, owner, "protected");
        Register(coordinator, owner, "current");
        using var current = Acquire(coordinator, owner, "current", 1);
        current.Acknowledge();
        var protectedAliases = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase) { "PROTECTED" };
        clock.Advance(_timeout);

        coordinator.RemoveExpired(protectedAliases);

        Assert.Equal(VideoPlaybackStatus.Expired, coordinator.ResolvePlaySession(owner.UserId, owner.SessionId, owner.ItemId, "obsolete", out _));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ResolvePlaySession(owner.UserId, owner.SessionId, owner.ItemId, "protected", out _));
        Assert.Equal(VideoPlaybackStatus.OwnershipMismatch, coordinator.RegisterPlaySession(owner with { UserId = Guid.NewGuid() }, "obsolete"));
        clock.Advance(_timeout);
        coordinator.RemoveExpired(protectedAliases);
        Assert.Equal(VideoPlaybackStatus.NotFound, coordinator.ResolvePlaySession(owner.UserId, owner.SessionId, owner.ItemId, "obsolete", out _));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ResolvePlaySession(owner.UserId, owner.SessionId, owner.ItemId, "current", out _));
    }

    [Fact]
    public void RemoveExpired_ProtectedRetiredAliasKeepsItsOwnerBinding()
    {
        var clock = new ManualTimeProvider();
        var coordinator = new VideoPlaybackCoordinator(clock, _timeout);
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");
        using (var request = Acquire(coordinator, owner, "encoding", 1))
        {
            request.Acknowledge();
        }

        Assert.Equal(VideoPlaybackStatus.Success, coordinator.Stop(owner, "encoding"));
        clock.Advance(_timeout + TimeSpan.FromTicks(1));
        coordinator.RemoveExpired(new System.Collections.Generic.HashSet<string> { "encoding" });
        Assert.Equal(VideoPlaybackStatus.Stopped, coordinator.ResolvePlaySession(owner.UserId, owner.SessionId, owner.ItemId, "encoding", out _));
        Assert.Equal(VideoPlaybackStatus.OwnershipMismatch, coordinator.RegisterPlaySession(owner with { UserId = Guid.NewGuid() }, "ENCODING"));
        coordinator.RemoveExpired();
        Assert.Equal(VideoPlaybackStatus.NotFound, coordinator.ResolvePlaySession(owner.UserId, owner.SessionId, owner.ItemId, "encoding", out _));
    }

    [Fact]
    public void CreateLegacyId_IsStableWithinAuthenticatedScope()
    {
        var owner = NewPlayback();
        var identifier = VideoPlaybackContext.CreateLegacyId(owner.UserId, owner.SessionId, owner.ItemId);

        Assert.Equal(identifier, VideoPlaybackContext.CreateLegacyId(owner.UserId, owner.SessionId, owner.ItemId));
        Assert.Matches("^legacy-[0-9a-f]{64}$", identifier);
        Assert.NotEqual(identifier, VideoPlaybackContext.CreateLegacyId(Guid.NewGuid(), owner.SessionId, owner.ItemId));
        Assert.NotEqual(identifier, VideoPlaybackContext.CreateLegacyId(owner.UserId, "other-session", owner.ItemId));
        Assert.NotEqual(identifier, VideoPlaybackContext.CreateLegacyId(owner.UserId, owner.SessionId, Guid.NewGuid()));
    }

    [Fact]
    public void RegisterPlaySession_FreshNegotiationExtendsMetadataLifetimeWithoutConsumingQuota()
    {
        var clock = new ManualTimeProvider();
        var coordinator = new VideoPlaybackCoordinator(clock, _timeout);
        var owner = NewPlayback();
        Register(coordinator, owner, "old-encoding");
        clock.Advance(_timeout / 2);
        Register(coordinator, owner, "fresh-encoding");
        clock.Advance((_timeout / 2) + TimeSpan.FromTicks(1));

        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ResolvePlaySession(owner.UserId, owner.SessionId, owner.ItemId, "fresh-encoding", out var resolved));
        Assert.Equal(owner, resolved);
        var other = owner with { PlaybackInstanceId = "other" };
        Register(coordinator, other, "other-encoding");
        using var request = Acquire(coordinator, other, "other-encoding", 1);
    }

    [Fact]
    public void RegisterPlaySession_FreshNegotiationKeepsAdmittedPlaybackAliveWithoutAnotherSlot()
    {
        var clock = new ManualTimeProvider();
        var coordinator = new VideoPlaybackCoordinator(clock, _timeout);
        var owner = NewPlayback();
        Register(coordinator, owner, "old-encoding");
        var original = Acquire(coordinator, owner, "old-encoding", 1);
        original.Acknowledge();
        original.Dispose();
        clock.Advance(_timeout / 2);
        Register(coordinator, owner, "fresh-encoding");
        clock.Advance((_timeout / 2) + TimeSpan.FromTicks(1));

        Assert.Equal(0, coordinator.RemoveExpired());
        using var continuation = Acquire(coordinator, owner, "fresh-encoding", 1);
        var other = owner with { PlaybackInstanceId = "other" };
        Register(coordinator, other, "other-encoding");
        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(other, "other-encoding", 1, out _));
    }

    [Fact]
    public void ResolvePlayback_SelectsActiveOrAcknowledgedAliasWithoutRegressingDuringOverlap()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "old-encoding");
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ResolvePlayback(owner, out var initial));
        Assert.Equal("old-encoding", initial);
        using var original = Acquire(coordinator, owner, "old-encoding", 1);
        original.Acknowledge();

        Register(coordinator, owner, "new-encoding");
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ResolvePlayback(owner, out var pending));
        Assert.Equal("old-encoding", pending);
        using var replacement = Acquire(coordinator, owner, "new-encoding", 1);
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ResolvePlayback(owner, out var unacknowledged));
        Assert.Equal("new-encoding", unacknowledged);
        replacement.Acknowledge();
        original.Acknowledge();

        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ResolvePlayback(owner, out var current));
        Assert.Equal("new-encoding", current);
        Assert.Equal(VideoPlaybackStatus.Superseded, coordinator.Stop(owner, "old-encoding"));
    }

    [Fact]
    public void ResolvePlayback_ActivePendingAliasTakesPrecedenceOverUnacquiredMetadata()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "active-pending");
        using var pending = Acquire(coordinator, owner, "active-pending", 1);
        Register(coordinator, owner, "metadata-only");

        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ResolvePlayback(owner, out var current));
        Assert.Equal("active-pending", current);
        Assert.Equal(VideoPlaybackStatus.NotFound, coordinator.Stop(owner, "metadata-only"));
    }

    [Fact]
    public void ResolvePlayback_MetadataOnlyReturnsLatestAliasWithoutAdmittingVideo()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "first");
        Register(coordinator, owner, "latest");

        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ResolvePlayback(owner, out var current));
        Assert.Equal("latest", current);
        var other = owner with { PlaybackInstanceId = "other" };
        Register(coordinator, other, "other-encoding");
        using var request = Acquire(coordinator, other, "other-encoding", 1);
    }

    [Theory]
    [InlineData("user", VideoPlaybackStatus.NotFound)]
    [InlineData("session", VideoPlaybackStatus.OwnershipMismatch)]
    [InlineData("item", VideoPlaybackStatus.OwnershipMismatch)]
    public void ResolvePlayback_RequiresMatchingLogicalOwner(string changedField, VideoPlaybackStatus expected)
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");
        var stranger = changedField switch
        {
            "user" => owner with { UserId = Guid.NewGuid() },
            "session" => owner with { SessionId = "other-owner" },
            _ => owner with { ItemId = Guid.NewGuid() }
        };

        Assert.Equal(expected, coordinator.ResolvePlayback(stranger, out var current));
        Assert.Null(current);
    }

    [Fact]
    public void ResolvePlayback_StoppedEpochDrainsBeforeFreshAliasCanRestart()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "old-encoding");
        var original = Acquire(coordinator, owner, "old-encoding", 1);
        original.Acknowledge();
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.Stop(owner, "old-encoding"));
        Assert.Equal(VideoPlaybackStatus.Stopped, coordinator.ResolvePlayback(owner, out var draining));
        Assert.Null(draining);
        Assert.Equal(VideoPlaybackStatus.Stopped, coordinator.RegisterPlaySession(owner, "new-encoding"));
        original.Dispose();

        Assert.Equal(VideoPlaybackStatus.NotFound, coordinator.ResolvePlayback(owner, out var removed));
        Assert.Null(removed);
        Register(coordinator, owner, "new-encoding");
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ResolvePlayback(owner, out var restarted));
        Assert.Equal("new-encoding", restarted);
        using var replacement = Acquire(coordinator, owner, "new-encoding", 1);
        replacement.Acknowledge();
        Assert.Equal(VideoPlaybackStatus.Stopped, coordinator.Stop(owner, "old-encoding"));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ReportActivity(owner, "new-encoding"));
    }

    [Fact]
    public void ResolvePlayback_ExpiredEpochAllowsFreshAliasButNotRetiredAliasReuse()
    {
        var clock = new ManualTimeProvider();
        var coordinator = new VideoPlaybackCoordinator(clock, _timeout);
        var owner = NewPlayback();
        Register(coordinator, owner, "old-encoding");
        clock.Advance(_timeout);

        Assert.Equal(VideoPlaybackStatus.Expired, coordinator.ResolvePlayback(owner, out var expired));
        Assert.Null(expired);
        Assert.Equal(VideoPlaybackStatus.Expired, coordinator.RegisterPlaySession(owner, "old-encoding"));
        Register(coordinator, owner, "new-encoding");
        using var replacement = Acquire(coordinator, owner, "new-encoding", 1);
    }

    [Fact]
    public void ResolvePlaySession_ReturnsCanonicalContextWithoutConsumingQuota()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");

        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ResolvePlaySession(owner.UserId, owner.SessionId, owner.ItemId, "encoding", out var resolved));
        Assert.Equal(owner, resolved);
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ResolvePlaySession(owner.UserId, owner.SessionId, null, "encoding", out var pingContext));
        Assert.Equal(owner, pingContext);

        var other = owner with { PlaybackInstanceId = "other" };
        Register(coordinator, other, "other-encoding");
        using var request = Acquire(coordinator, other, "other-encoding", 1);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("session")]
    [InlineData("item")]
    public void ResolvePlaySession_RejectsMismatchedOwnerOrItem(string changedField)
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");
        var userId = changedField == "user" ? Guid.NewGuid() : owner.UserId;
        var sessionId = changedField == "session" ? "other-session" : owner.SessionId;
        var itemId = changedField == "item" ? Guid.NewGuid() : owner.ItemId;

        Assert.Equal(VideoPlaybackStatus.OwnershipMismatch, coordinator.ResolvePlaySession(userId, sessionId, itemId, "encoding", out var resolved));
        Assert.Equal(default, resolved);
    }

    [Fact]
    public void ResolvePlaySession_UnknownAliasDoesNotCreateMetadata()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();

        Assert.Equal(VideoPlaybackStatus.NotFound, coordinator.ResolvePlaySession(owner.UserId, owner.SessionId, owner.ItemId, "unknown", out var resolved));
        Assert.Equal(default, resolved);
        Assert.Equal(VideoPlaybackStatus.NotFound, coordinator.TryAcquire(owner, "unknown", 1, out _));
    }

    [Theory]
    [InlineData(false, VideoPlaybackStatus.Expired)]
    [InlineData(true, VideoPlaybackStatus.Stopped)]
    public void ResolvePlaySession_TerminalAliasDoesNotReturnLiveContext(bool stopped, VideoPlaybackStatus expected)
    {
        var clock = new ManualTimeProvider();
        var coordinator = new VideoPlaybackCoordinator(clock, _timeout);
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");
        if (stopped)
        {
            using var request = Acquire(coordinator, owner, "encoding", 1);
            request.Acknowledge();
            Assert.Equal(VideoPlaybackStatus.Success, coordinator.Stop(owner, "encoding"));
        }
        else
        {
            clock.Advance(_timeout);
        }

        Assert.Equal(expected, coordinator.ResolvePlaySession(owner.UserId, owner.SessionId, owner.ItemId, "encoding", out var resolved));
        Assert.Equal(default, resolved);
    }

    [Fact]
    public async Task TryAcquire_ParallelRequestsForSamePlayback_ConsumeOneSlot()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");

        var requests = await RunTogether(12, _ => Acquire(coordinator, owner, "encoding", 2));
        var second = owner with { PlaybackInstanceId = "second" };
        Register(coordinator, second, "second-encoding");
        using var secondRequest = Acquire(coordinator, second, "second-encoding", 2);
        var third = owner with { PlaybackInstanceId = "third" };
        Register(coordinator, third, "third-encoding");
        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(third, "third-encoding", 2, out var denied));
        Assert.Null(denied);

        foreach (var request in requests)
        {
            request.Dispose();
        }
    }

    [Fact]
    public void TryAcquire_AnotherUserHasIndependentQuota()
    {
        var coordinator = CreateCoordinator();
        var first = NewPlayback();
        var second = first with { UserId = Guid.NewGuid() };
        Register(coordinator, first, "first");
        Register(coordinator, second, "second");

        using var firstRequest = Acquire(coordinator, first, "first", 1);
        using var secondRequest = Acquire(coordinator, second, "second", 1);
    }

    [Theory]
    [InlineData("user")]
    [InlineData("session")]
    [InlineData("item")]
    [InlineData("instance")]
    public void RegisteredPlayback_RejectsMismatchedOwnership(string changedField)
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        var stranger = changedField switch
        {
            "user" => owner with { UserId = Guid.NewGuid() },
            "session" => owner with { SessionId = "other-session" },
            "item" => owner with { ItemId = Guid.NewGuid() },
            _ => owner with { PlaybackInstanceId = "other-instance" }
        };
        Register(coordinator, owner, "encoding");

        Assert.Equal(VideoPlaybackStatus.OwnershipMismatch, coordinator.RegisterPlaySession(stranger, "encoding"));
        Assert.Equal(VideoPlaybackStatus.OwnershipMismatch, coordinator.TryAcquire(stranger, "encoding", 1, out var denied));
        Assert.Null(denied);
        Assert.Equal(VideoPlaybackStatus.OwnershipMismatch, coordinator.ReportActivity(stranger, "encoding"));
        Assert.Equal(VideoPlaybackStatus.OwnershipMismatch, coordinator.Stop(stranger, "encoding"));
        using var request = Acquire(coordinator, owner, "encoding", 1);
    }

    [Fact]
    public void TryAcquire_RequiresRegisteredMetadata()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();

        Assert.Equal(VideoPlaybackStatus.NotFound, coordinator.TryAcquire(owner, "unknown", 1, out var request));
        Assert.Null(request);
        Assert.Equal(VideoPlaybackStatus.NotFound, coordinator.ReportActivity(owner, "unknown"));
        Assert.Equal(VideoPlaybackStatus.NotFound, coordinator.Stop(owner, "unknown"));
    }

    [Theory]
    [InlineData("session")]
    [InlineData("item")]
    public void RegisterPlaySession_NewAliasCannotRebindLivePlaybackInstance(string changedField)
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        var stranger = changedField switch
        {
            "session" => owner with { SessionId = "other-session" },
            _ => owner with { ItemId = Guid.NewGuid() }
        };
        Register(coordinator, owner, "encoding");

        Assert.Equal(VideoPlaybackStatus.OwnershipMismatch, coordinator.RegisterPlaySession(stranger, "new-encoding"));
        Assert.Equal(VideoPlaybackStatus.NotFound, coordinator.TryAcquire(stranger, "new-encoding", 1, out _));
        using var request = Acquire(coordinator, owner, "encoding", 1);
    }

    [Fact]
    public void RegisterPlaySession_RepeatedRegistrationDoesNotResetAdmission()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");
        var request = Acquire(coordinator, owner, "encoding", 1);
        request.Acknowledge();
        request.Dispose();

        Register(coordinator, owner, "encoding");
        using var continuation = Acquire(coordinator, owner, "encoding", 1);
        var other = owner with { PlaybackInstanceId = "other" };
        Register(coordinator, other, "other-encoding");

        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(other, "other-encoding", 1, out _));
    }

    [Fact]
    public void NewEncodingAlias_ReusesLogicalSlotAndOldStopCannotStopIt()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "old-encoding");
        using var original = Acquire(coordinator, owner, "old-encoding", 1);
        original.Acknowledge();
        Register(coordinator, owner, "new-encoding");

        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ReportActivity(owner, "old-encoding"));
        using var replacement = Acquire(coordinator, owner, "new-encoding", 1);
        replacement.Acknowledge();
        using var overlap = Acquire(coordinator, owner, "old-encoding", 1);
        overlap.Acknowledge();
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ReportActivity(owner, "old-encoding"));
        Assert.Equal(VideoPlaybackStatus.Superseded, coordinator.Stop(owner, "old-encoding"));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ReportActivity(owner, "new-encoding"));

        var other = owner with { PlaybackInstanceId = "other" };
        Register(coordinator, other, "other-encoding");
        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(other, "other-encoding", 1, out _));
    }

    [Fact]
    public void Stop_RegisteredButUnstartedAliasCannotStopCurrentPlayback()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "current-encoding");
        Register(coordinator, owner, "unstarted-encoding");
        using var current = Acquire(coordinator, owner, "current-encoding", 1);
        current.Acknowledge();

        Assert.Equal(VideoPlaybackStatus.NotFound, coordinator.Stop(owner, "unstarted-encoding"));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ReportActivity(owner, "current-encoding"));
    }

    [Fact]
    public void PlaySessionIds_UseTranscoderCaseMatchingAndRetainOwnership()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        var stranger = owner with { UserId = Guid.NewGuid(), PlaybackInstanceId = "other" };
        const string encoding = "0123456789abcdef0123456789abcdef";
        var upperCase = encoding.ToUpperInvariant();
        Register(coordinator, owner, encoding);

        Assert.Equal(VideoPlaybackStatus.OwnershipMismatch, coordinator.RegisterPlaySession(stranger, upperCase));
        Assert.Equal(VideoPlaybackStatus.OwnershipMismatch, coordinator.ResolvePlaySession(stranger.UserId, stranger.SessionId, stranger.ItemId, upperCase, out _));
        Assert.Equal(VideoPlaybackStatus.OwnershipMismatch, coordinator.TryAcquire(stranger, upperCase, 1, out _));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ResolvePlaySession(owner.UserId, owner.SessionId, owner.ItemId, upperCase, out var resolved));
        Assert.Equal(owner, resolved);
        using var request = Acquire(coordinator, owner, upperCase, 1);
        using var duplicate = Acquire(coordinator, owner, encoding, 1);
    }

    [Fact]
    public void Stop_CurrentMetadataOnlyAliasCanCancelWithoutConsumingQuota()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "old-metadata");
        Register(coordinator, owner, "current-metadata");

        Assert.Equal(VideoPlaybackStatus.Superseded, coordinator.Stop(owner, "old-metadata"));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.Stop(owner, "current-metadata"));
        Assert.Equal(VideoPlaybackStatus.Stopped, coordinator.ResolvePlaySession(owner.UserId, owner.SessionId, owner.ItemId, "current-metadata", out _));
        var other = owner with { PlaybackInstanceId = "other" };
        Register(coordinator, other, "other-encoding");
        using var request = Acquire(coordinator, other, "other-encoding", 1);
    }

    [Fact]
    public void Stop_PendingReplacementProtectsCurrentPlaybackAndDrainsBeforeReleasingQuota()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "old-encoding");
        var original = Acquire(coordinator, owner, "old-encoding", 1);
        original.Acknowledge();
        Register(coordinator, owner, "pending-encoding");
        var pending = Acquire(coordinator, owner, "pending-encoding", 1);

        Assert.Equal(VideoPlaybackStatus.Superseded, coordinator.Stop(owner, "old-encoding"));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.Stop(owner, "pending-encoding"));
        var other = owner with { PlaybackInstanceId = "other" };
        Register(coordinator, other, "other-encoding");
        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(other, "other-encoding", 1, out _));
        pending.Dispose();
        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(other, "other-encoding", 1, out _));
        original.Dispose();
        using var next = Acquire(coordinator, other, "other-encoding", 1);
    }

    [Fact]
    public void FailedReplacement_DoesNotSupersedeCurrentPlaybackStop()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "current-encoding");
        var current = Acquire(coordinator, owner, "current-encoding", 1);
        current.Acknowledge();
        current.Dispose();
        Register(coordinator, owner, "failed-encoding");
        var failedReplacement = Acquire(coordinator, owner, "failed-encoding", 1);
        failedReplacement.Dispose();

        Assert.Equal(VideoPlaybackStatus.Success, coordinator.Stop(owner, "current-encoding"));
        var next = owner with { PlaybackInstanceId = "next" };
        Register(coordinator, next, "next-encoding");
        using var admitted = Acquire(coordinator, next, "next-encoding", 1);
    }

    [Fact]
    public void DisposingUnacknowledgedRequest_RollsBackAdmission()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");
        var abandoned = Acquire(coordinator, owner, "encoding", 1);

        abandoned.Dispose();
        abandoned.Dispose();
        var retry = Acquire(coordinator, owner, "encoding", 1);
        retry.Dispose();

        var next = owner with { PlaybackInstanceId = "next" };
        Register(coordinator, next, "next-encoding");
        using var admitted = Acquire(coordinator, next, "next-encoding", 1);
    }

    [Fact]
    public void RetainedRequest_KeepsPendingSlotUntilLastReferenceIsDisposed()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");
        var initial = Acquire(coordinator, owner, "encoding", 1);
        var retained = initial.Retain();
        initial.Dispose();

        var next = owner with { PlaybackInstanceId = "next" };
        Register(coordinator, next, "next-encoding");
        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(next, "next-encoding", 1, out _));

        retained.Dispose();
        using var admitted = Acquire(coordinator, next, "next-encoding", 1);
    }

    [Fact]
    public void CancellingOnePendingRequest_DoesNotRollbackAcknowledgedSibling()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");
        var cancelled = Acquire(coordinator, owner, "encoding", 1);
        using var sibling = Acquire(coordinator, owner, "encoding", 1);
        sibling.Acknowledge();
        sibling.Dispose();
        cancelled.Dispose();

        var next = owner with { PlaybackInstanceId = "next" };
        Register(coordinator, next, "next-encoding");
        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(next, "next-encoding", 1, out _));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ReportActivity(owner, "encoding"));
    }

    [Fact]
    public void AcknowledgedPlayback_RemainsAdmittedAfterRequestIsDisposed()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");
        var request = Acquire(coordinator, owner, "encoding", 1);
        request.Acknowledge();
        request.Dispose();
        request.Dispose();

        using var continuation = Acquire(coordinator, owner, "encoding", 1);
        var next = owner with { PlaybackInstanceId = "next" };
        Register(coordinator, next, "next-encoding");
        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(next, "next-encoding", 1, out _));
    }

    [Fact]
    public void Stop_PreventsNewRequestsAndKeepsSlotUntilExistingRequestsDrain()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");
        var request = Acquire(coordinator, owner, "encoding", 1);
        request.Acknowledge();
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.Stop(owner, "encoding"));
        Assert.Equal(VideoPlaybackStatus.Stopped, coordinator.TryAcquire(owner, "encoding", 1, out _));
        Assert.Throws<InvalidOperationException>(() => request.Retain());

        var next = owner with { PlaybackInstanceId = "next" };
        Register(coordinator, next, "next-encoding");
        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(next, "next-encoding", 1, out _));
        request.Dispose();
        using var admitted = Acquire(coordinator, next, "next-encoding", 1);
    }

    [Fact]
    public void OldCallbacks_CannotChangeNewGenerationOfSamePlayback()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "old-encoding");
        var oldRequest = Acquire(coordinator, owner, "old-encoding", 1);
        oldRequest.Acknowledge();
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.Stop(owner, "old-encoding"));
        oldRequest.Dispose();

        Register(coordinator, owner, "new-encoding");
        using var newRequest = Acquire(coordinator, owner, "new-encoding", 1);
        newRequest.Acknowledge();

        Assert.Throws<ObjectDisposedException>(oldRequest.Acknowledge);
        Assert.Throws<ObjectDisposedException>(() => oldRequest.Retain());
        oldRequest.Dispose();
        Assert.Equal(VideoPlaybackStatus.Stopped, coordinator.Stop(owner, "old-encoding"));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ReportActivity(owner, "new-encoding"));
        var other = owner with { PlaybackInstanceId = "other" };
        Register(coordinator, other, "other-encoding");
        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(other, "other-encoding", 1, out _));
    }

    [Fact]
    public async Task StopAndDispose_ConcurrentCallsReleaseExactlyOneSlot()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");
        var request = Acquire(coordinator, owner, "encoding", 1);
        request.Acknowledge();

        var results = await RunTogether(2, index =>
        {
            if (index == 0)
            {
                return coordinator.Stop(owner, "encoding");
            }

            request.Dispose();
            return VideoPlaybackStatus.Success;
        });
        Assert.All(results, result => Assert.Equal(VideoPlaybackStatus.Success, result));

        var next = owner with { PlaybackInstanceId = "next" };
        Register(coordinator, next, "next-encoding");
        using var admitted = Acquire(coordinator, next, "next-encoding", 1);
        request.Dispose();
        Assert.Equal(VideoPlaybackStatus.Stopped, coordinator.Stop(owner, "encoding"));
        var third = owner with { PlaybackInstanceId = "third" };
        Register(coordinator, third, "third-encoding");

        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(third, "third-encoding", 1, out _));
    }

    [Fact]
    public async Task ExpiryAndActivity_ConcurrentCallsCannotRenewExpiredPlaybackOrReleaseQuotaTwice()
    {
        var clock = new ManualTimeProvider();
        var coordinator = new VideoPlaybackCoordinator(clock, _timeout);
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");
        var request = Acquire(coordinator, owner, "encoding", 1);
        request.Acknowledge();
        request.Dispose();
        clock.Advance(_timeout);

        var results = await RunTogether(2, index =>
        {
            if (index == 0)
            {
                coordinator.RemoveExpired();
                return VideoPlaybackStatus.Success;
            }

            return coordinator.ReportActivity(owner, "encoding");
        });
        Assert.Equal(VideoPlaybackStatus.Expired, results[1]);

        var next = owner with { PlaybackInstanceId = "next" };
        Register(coordinator, next, "next-encoding");
        using var admitted = Acquire(coordinator, next, "next-encoding", 1);
        Assert.Equal(VideoPlaybackStatus.Expired, coordinator.RegisterPlaySession(owner, "encoding"));
        Assert.Equal(VideoPlaybackStatus.Expired, coordinator.TryAcquire(owner, "encoding", 1, out _));
        request.Dispose();
        var third = owner with { PlaybackInstanceId = "third" };
        Register(coordinator, third, "third-encoding");

        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(third, "third-encoding", 1, out _));
    }

    [Fact]
    public void InactivityExpiration_UsesMonotonicTimeAndReleasesIdleSlot()
    {
        var clock = new ManualTimeProvider();
        var coordinator = new VideoPlaybackCoordinator(clock, _timeout);
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");
        var request = Acquire(coordinator, owner, "encoding", 1);
        request.Acknowledge();
        request.Dispose();

        clock.ShiftUtc(TimeSpan.FromDays(100));
        Assert.Equal(0, coordinator.RemoveExpired());
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ReportActivity(owner, "encoding"));
        clock.ShiftUtc(TimeSpan.FromDays(-200));
        clock.Advance(_timeout + TimeSpan.FromTicks(1));
        Assert.True(coordinator.RemoveExpired() > 0);

        var next = owner with { PlaybackInstanceId = "next" };
        Register(coordinator, next, "next-encoding");
        using var admitted = Acquire(coordinator, next, "next-encoding", 1);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActiveRequestLease_CannotExpireWhileResourceIsHeld(bool acknowledged)
    {
        var clock = new ManualTimeProvider();
        var coordinator = new VideoPlaybackCoordinator(clock, _timeout);
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");
        var active = Acquire(coordinator, owner, "encoding", 1);
        if (acknowledged)
        {
            active.Acknowledge();
        }

        clock.Advance(_timeout + TimeSpan.FromTicks(1));
        coordinator.RemoveExpired();

        var next = owner with { PlaybackInstanceId = "next" };
        Register(coordinator, next, "next-encoding");
        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(next, "next-encoding", 1, out _));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ReportActivity(owner, "encoding"));
        active.Dispose();
        clock.Advance(_timeout + TimeSpan.FromTicks(1));
        coordinator.RemoveExpired();
        var fresh = owner with { PlaybackInstanceId = "fresh" };
        Register(coordinator, fresh, "fresh-encoding");
        using var admitted = Acquire(coordinator, fresh, "fresh-encoding", 1);
    }

    [Fact]
    public void MetadataExpiration_DoesNotAuthorizePlayback()
    {
        var clock = new ManualTimeProvider();
        var coordinator = new VideoPlaybackCoordinator(clock, _timeout);
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");
        clock.Advance(_timeout + TimeSpan.FromTicks(1));

        Assert.Equal(VideoPlaybackStatus.Expired, coordinator.TryAcquire(owner, "encoding", 1, out var request));
        Assert.Null(request);
        Assert.Equal(VideoPlaybackStatus.Expired, coordinator.RegisterPlaySession(owner, "encoding"));
    }

    [Fact]
    public void ReportActivity_AcquiredBodyRemainsAdmittedAfterAbortWithoutCompletionAcknowledgement()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");
        var response = Acquire(coordinator, owner, "encoding", 1);

        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ReportActivity(owner, "encoding"));
        response.Dispose();

        var other = owner with { PlaybackInstanceId = "other" };
        Register(coordinator, other, "other-encoding");
        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(other, "other-encoding", 1, out _));
        using var seek = Acquire(coordinator, owner, "encoding", 1);
    }

    [Fact]
    public void ReportActivity_MetadataAliasCannotConfirmOrSupersedeAnExistingAdmission()
    {
        var coordinator = CreateCoordinator();
        var owner = NewPlayback();
        Register(coordinator, owner, "old-encoding");
        var original = Acquire(coordinator, owner, "old-encoding", 1);
        original.Acknowledge();
        original.Dispose();
        Register(coordinator, owner, "metadata-only-encoding");

        Assert.Equal(VideoPlaybackStatus.NotFound, coordinator.ReportActivity(owner, "metadata-only-encoding"));
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ResolvePlayback(owner, out var current));
        Assert.Equal("old-encoding", current);
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.Stop(owner, "old-encoding"));
    }

    [Fact]
    public void ReportActivity_RefreshesAcknowledgedPlaybackTimeout()
    {
        var clock = new ManualTimeProvider();
        var coordinator = new VideoPlaybackCoordinator(clock, _timeout);
        var owner = NewPlayback();
        Register(coordinator, owner, "encoding");
        var request = Acquire(coordinator, owner, "encoding", 1);
        request.Acknowledge();
        request.Dispose();

        clock.Advance(_timeout / 2);
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.ReportActivity(owner, "encoding"));
        clock.Advance((_timeout / 2) + TimeSpan.FromTicks(1));

        Assert.Equal(0, coordinator.RemoveExpired());
        using var continuation = Acquire(coordinator, owner, "encoding", 1);
    }

    [Fact]
    public void LoweringLimit_DoesNotInterruptExistingPlaybacks()
    {
        var coordinator = CreateCoordinator();
        var first = NewPlayback();
        var second = first with { PlaybackInstanceId = "second" };
        var third = first with { PlaybackInstanceId = "third" };
        Register(coordinator, first, "first");
        Register(coordinator, second, "second");
        Register(coordinator, third, "third");
        using var firstRequest = Acquire(coordinator, first, "first", 2);
        using var secondRequest = Acquire(coordinator, second, "second", 2);
        firstRequest.Acknowledge();
        secondRequest.Acknowledge();
        firstRequest.Dispose();
        secondRequest.Dispose();
        using var firstContinuation = Acquire(coordinator, first, "first", 1);
        using var secondContinuation = Acquire(coordinator, second, "second", 1);

        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(third, "third", 1, out _));
    }

    [Fact]
    public void ChangingUnlimitedToOne_CountsAlreadyAdmittedPlaybacks()
    {
        var coordinator = CreateCoordinator();
        var first = NewPlayback();
        var second = first with { PlaybackInstanceId = "second" };
        Register(coordinator, first, "first");
        Register(coordinator, second, "second");
        using var firstRequest = Acquire(coordinator, first, "first", 0);
        firstRequest.Acknowledge();
        firstRequest.Dispose();

        Assert.Equal(VideoPlaybackStatus.LimitReached, coordinator.TryAcquire(second, "second", 1, out _));
        using var continuation = Acquire(coordinator, first, "first", 1);
    }

    private static VideoPlaybackCoordinator CreateCoordinator() => new(new ManualTimeProvider(), _timeout);

    private static VideoPlaybackContext NewPlayback() => new(Guid.NewGuid(), "session", Guid.NewGuid(), "playback");

    private static void Register(VideoPlaybackCoordinator coordinator, VideoPlaybackContext playback, string playSessionId)
        => Assert.Equal(VideoPlaybackStatus.Success, coordinator.RegisterPlaySession(playback, playSessionId));

    private static IVideoPlaybackRequest Acquire(VideoPlaybackCoordinator coordinator, VideoPlaybackContext playback, string playSessionId, int limit)
    {
        Assert.Equal(VideoPlaybackStatus.Success, coordinator.TryAcquire(playback, playSessionId, limit, out var request));
        return Assert.IsAssignableFrom<IVideoPlaybackRequest>(request);
    }

    private static async Task<T[]> RunTogether<T>(int count, Func<int, T> action)
    {
        var ready = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var readyCount = 0;
        var cancellationToken = TestContext.Current.CancellationToken;
        var tasks = Enumerable.Range(0, count).Select(index => Task.Run(
            async () =>
            {
                if (Interlocked.Increment(ref readyCount) == count)
                {
                    ready.SetResult();
                }

                await start.Task.WaitAsync(cancellationToken);
                return action(index);
            },
            cancellationToken)).ToArray();

        await ready.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        start.SetResult();
        return await Task.WhenAll(tasks);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private long _timestamp;
        private long _utcTicks = DateTimeOffset.UnixEpoch.Ticks;

        public override long TimestampFrequency => TimeSpan.TicksPerSecond;

        public override long GetTimestamp() => Volatile.Read(ref _timestamp);

        public override DateTimeOffset GetUtcNow() => new(Volatile.Read(ref _utcTicks), TimeSpan.Zero);

        public void Advance(TimeSpan elapsed)
        {
            Interlocked.Add(ref _timestamp, elapsed.Ticks);
            ShiftUtc(elapsed);
        }

        public void ShiftUtc(TimeSpan elapsed) => Interlocked.Add(ref _utcTicks, elapsed.Ticks);
    }
}

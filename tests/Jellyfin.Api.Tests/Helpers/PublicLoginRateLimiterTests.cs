using System;
using Jellyfin.Api.Helpers;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers;

public sealed class PublicLoginRateLimiterTests
{
    [Fact]
    public void RecordFailure_AppliesProgressiveBackoff()
    {
        var timeProvider = new ManualTimeProvider();
        var limiter = new PublicLoginRateLimiter(timeProvider);

        limiter.RecordFailure("203.0.113.1", "alice", true, 2, 900);
        Assert.False(limiter.IsBlocked("203.0.113.1", "alice", true, 900, out _));

        limiter.RecordFailure("203.0.113.1", "alice", true, 2, 900);
        Assert.True(limiter.IsBlocked("203.0.113.1", "alice", true, 900, out var firstBackoff));
        Assert.Equal(TimeSpan.FromSeconds(1), firstBackoff);

        timeProvider.Advance(TimeSpan.FromSeconds(1));
        limiter.RecordFailure("203.0.113.1", "alice", true, 2, 900);
        Assert.True(limiter.IsBlocked("203.0.113.1", "alice", true, 900, out var secondBackoff));
        Assert.Equal(TimeSpan.FromSeconds(2), secondBackoff);
    }

    [Fact]
    public void RecordFailure_BlocksAccountAcrossAddresses()
    {
        var limiter = new PublicLoginRateLimiter(new ManualTimeProvider());

        limiter.RecordFailure("203.0.113.2", "alice", true, 1, 900);

        Assert.True(limiter.IsBlocked("203.0.113.3", "ALICE", true, 900, out _));
    }

    [Fact]
    public void RecordFailure_WhenAccountKeyExcluded_DoesNotBlockAnotherAddress()
    {
        var limiter = new PublicLoginRateLimiter(new ManualTimeProvider());

        limiter.RecordFailure("203.0.113.4", "administrator", false, 1, 900);

        Assert.False(limiter.IsBlocked("203.0.113.5", "administrator", false, 900, out _));
    }

    [Fact]
    public void RecordSuccess_ClearsAccountButKeepsAddressBackoff()
    {
        var limiter = new PublicLoginRateLimiter(new ManualTimeProvider());
        limiter.RecordFailure("203.0.113.6", "alice", true, 1, 900);

        limiter.RecordSuccess("alice");

        Assert.True(limiter.IsBlocked("203.0.113.6", "alice", true, 900, out _));
        Assert.False(limiter.IsBlocked("203.0.113.7", "alice", true, 900, out _));
    }

    [Fact]
    public void RecordFailure_AfterWindowExpires_StartsANewSequence()
    {
        var timeProvider = new ManualTimeProvider();
        var limiter = new PublicLoginRateLimiter(timeProvider);
        limiter.RecordFailure("203.0.113.8", "alice", true, 2, 900);
        timeProvider.Advance(TimeSpan.FromSeconds(900));

        limiter.RecordFailure("203.0.113.8", "alice", true, 2, 900);

        Assert.False(limiter.IsBlocked("203.0.113.8", "alice", true, 900, out _));
    }

    [Fact]
    public void RecordFailure_KeepsTrackedKeysBounded()
    {
        var limiter = new PublicLoginRateLimiter(new ManualTimeProvider());

        for (var index = 0; index < 5_100; index++)
        {
            limiter.RecordFailure($"203.0.113.{index}", $"user-{index}", true, 5, 900);
        }

        Assert.InRange(limiter.TrackedKeyCount, 1, 10_000);
    }

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow()
            => _utcNow;

        public void Advance(TimeSpan duration)
        {
            _utcNow += duration;
        }
    }
}

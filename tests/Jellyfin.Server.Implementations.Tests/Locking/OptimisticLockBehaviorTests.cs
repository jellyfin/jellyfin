using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Locking;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using Polly.Utilities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Locking;

/// <summary>
/// The retries of <see cref="OptimisticLockBehavior"/> when the database stays locked. Polly's clock records the
/// waits instead of sleeping through them, and the save hooks never use the context, so none is created.
/// </summary>
public sealed class OptimisticLockBehaviorTests : IDisposable
{
    private static readonly TimeSpan[] _sleepDurations =
    [
        TimeSpan.FromMilliseconds(50),
        TimeSpan.FromMilliseconds(50),
        TimeSpan.FromMilliseconds(50),
        TimeSpan.FromMilliseconds(50),
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromMilliseconds(150),
        TimeSpan.FromMilliseconds(150),
        TimeSpan.FromMilliseconds(150),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromMilliseconds(150),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromMilliseconds(150),
        TimeSpan.FromSeconds(3)
    ];

    private readonly List<TimeSpan> _waits = [];
    private readonly OptimisticLockBehavior _behavior = new(NullLogger<OptimisticLockBehavior>.Instance);

    public OptimisticLockBehaviorTests()
    {
        SystemClock.Sleep = (wait, _) => _waits.Add(wait);
        SystemClock.SleepAsync = (wait, _) =>
        {
            _waits.Add(wait);
            return Task.CompletedTask;
        };
    }

    public void Dispose()
    {
        SystemClock.Reset();
    }

    [Fact]
    public void OnSaveChanges_DatabaseLockedBriefly_WaitsTheConfiguredDurationsInOrder()
    {
        var attempts = 0;

        _behavior.OnSaveChanges(null!, () =>
        {
            if (++attempts <= 5)
            {
                throw DatabaseLocked();
            }
        });

        Assert.Equal(6, attempts);
        AssertWaits(5);
    }

    [Fact]
    public void OnSaveChanges_DatabaseStaysLocked_ThrowsTheDatabaseErrorAfterTheLastRetry()
    {
        var attempts = 0;

        Assert.Throws<SqliteException>(() => _behavior.OnSaveChanges(null!, () =>
        {
            attempts++;
            throw DatabaseLocked();
        }));

        Assert.Equal(_sleepDurations.Length + 1, attempts);
        AssertWaits(_sleepDurations.Length);
    }

    [Fact]
    public async Task OnSaveChangesAsync_DatabaseStaysLocked_ThrowsTheDatabaseErrorAfterTheLastRetry()
    {
        var attempts = 0;

        await Assert.ThrowsAsync<SqliteException>(() => _behavior.OnSaveChangesAsync(null!, () =>
        {
            attempts++;
            throw DatabaseLocked();
        }));

        Assert.Equal(_sleepDurations.Length + 1, attempts);
        AssertWaits(_sleepDurations.Length);
    }

    private static SqliteException DatabaseLocked() => new("SQLite Error 5: 'database is locked'.", 5);

    private void AssertWaits(int retries)
    {
        Assert.Equal(retries, _waits.Count);
        for (var i = 0; i < retries; i++)
        {
            // The configured duration plus a jitter of less than half of it.
            Assert.InRange(_waits[i], _sleepDurations[i], _sleepDurations[i] * 1.5);
        }
    }
}

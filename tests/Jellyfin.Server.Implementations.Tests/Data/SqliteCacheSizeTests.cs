using Jellyfin.Database.Providers.Sqlite;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

/// <summary>
/// Regression tests for the default SQLite <c>PRAGMA cache_size</c> (jellyfin/jellyfin#17405).
/// Before the fix the <c>cacheSize</c> option defaulted to <c>null</c>, so no <c>PRAGMA cache_size</c>
/// was emitted and SQLite used its 2 MiB default — which thrashes on the ItemsByName queries on
/// large libraries. These tests guard the default from silently regressing back to "unset".
/// </summary>
public static class SqliteCacheSizeTests
{
    [Fact]
    public static void GetDefaultCacheSize_IsNegative_SoItIsInterpretedAsKiBAndAlwaysEmitted()
    {
        // A negative value means "size in KiB" to SQLite and, crucially, is non-null so the
        // interceptor actually emits `PRAGMA cache_size`. The pre-fix bug was this being unset.
        Assert.True(SqliteDatabaseProvider.GetDefaultCacheSize() < 0);
    }

    [Fact]
    public static void GetDefaultCacheSize_IsClampedToSaneRange()
    {
        // KiB, negated. Must stay within [16 MiB, 128 MiB] regardless of host memory:
        // small enough not to hurt a Raspberry Pi, large enough to hold the ItemsByName working set.
        const int MinKiB = 16 * 1024;
        const int MaxKiB = 128 * 1024;

        var sizeKiB = -SqliteDatabaseProvider.GetDefaultCacheSize();

        Assert.InRange(sizeKiB, MinKiB, MaxKiB);
    }

    [Fact]
    public static void GetDefaultCacheSize_IsDeterministic()
    {
        // Same process/host => same value on every connection open.
        Assert.Equal(SqliteDatabaseProvider.GetDefaultCacheSize(), SqliteDatabaseProvider.GetDefaultCacheSize());
    }
}

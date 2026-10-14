using System;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.ValueConverters;
using Jellyfin.Server.Implementations.Tests.Data;
using Jellyfin.Database.Providers.MySql;
using Jellyfin.Database.Testing;
using MediaBrowser.Controller.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The shared database context stores a <see cref="DateTime"/> as UTC whatever its <see cref="DateTimeKind"/>,
/// and SQLite reads it back as UTC. Values are compared to within a microsecond,
/// as the stored precision is not under test.
/// </summary>
[Collection(TimeZoneIsolation.Name)]
public sealed class DateTimeRoundTripTests : JellyfinDbTestFixture
{
    private static readonly TimeSpan _microsecond = TimeSpan.FromTicks(10);

    public static TheoryData<DateTime> Sentinels() => new()
    {
        DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc),
        DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
    };

    [Theory]
    [InlineData(DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void DateTime_RoundTrip_IsUtc(DateTimeKind kind)
    {
        // A tick count that is not a multiple of 10, so the value has digits below a microsecond.
        var input = new DateTime(2021, 7, 15, 12, 34, 56, kind).AddTicks(1234567);
        var expected = input.ToUniversalTime();

        var (dateCreated, premiereDate, activityDateCreated) = RoundTrip(input);

        AssertUtc(expected, dateCreated);
        AssertUtc(expected, premiereDate);
        AssertUtc(expected, activityDateCreated);
    }

    [Theory]
    [MemberData(nameof(Sentinels))]
    public void DateTime_Sentinels_RoundTripExactly(DateTime sentinel)
    {
        var (dateCreated, premiereDate, activityDateCreated) = RoundTrip(sentinel);

        Assert.Equal(sentinel, dateCreated);
        Assert.Equal(sentinel, premiereDate);
        Assert.Equal(sentinel, activityDateCreated);
        Assert.Equal(DateTimeKind.Utc, dateCreated!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, premiereDate!.Value.Kind);
        Assert.Equal(DateTimeKind.Utc, activityDateCreated.Kind);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void DateTime_MinValueOfOtherKind_RoundTripsExactly(DateTimeKind kind)
    {
        var sentinel = new DateTime(DateTime.MinValue.Ticks, kind);

        var (dateCreated, premiereDate, activityDateCreated) = RoundTrip(sentinel);

        AssertSentinel(DateTime.MinValue.Ticks, dateCreated);
        AssertSentinel(DateTime.MinValue.Ticks, premiereDate);
        AssertSentinel(DateTime.MinValue.Ticks, activityDateCreated);
    }

    [Theory]
    [InlineData(DateTimeKind.Local)]
    [InlineData(DateTimeKind.Unspecified)]
    public void DateTime_MaxValueOfOtherKind_RoundTripsExactly(DateTimeKind kind)
    {
        var sentinel = new DateTime(DateTime.MaxValue.Ticks, kind);

        var (dateCreated, premiereDate, activityDateCreated) = RoundTrip(sentinel);

        AssertSentinel(DateTime.MaxValue.Ticks, dateCreated);
        AssertSentinel(DateTime.MaxValue.Ticks, premiereDate);
        AssertSentinel(DateTime.MaxValue.Ticks, activityDateCreated);
    }

    [Theory]
    [InlineData("America/New_York", 0, DateTimeKind.Local)]
    [InlineData("America/New_York", 0, DateTimeKind.Unspecified)]
    [InlineData("America/New_York", 3155378975999999999, DateTimeKind.Local)]
    [InlineData("America/New_York", 3155378975999999999, DateTimeKind.Unspecified)]
    [InlineData("Asia/Kolkata", 0, DateTimeKind.Local)]
    [InlineData("Asia/Kolkata", 0, DateTimeKind.Unspecified)]
    [InlineData("Asia/Kolkata", 3155378975999999999, DateTimeKind.Local)]
    [InlineData("Asia/Kolkata", 3155378975999999999, DateTimeKind.Unspecified)]
    public void DateTime_SentinelOfOtherKindInZone_RoundTripsExactly(string zoneId, long ticks, DateTimeKind kind)
    {
        using var zone = new TemporaryTimeZone(zoneId);

        var (dateCreated, premiereDate, activityDateCreated) = RoundTrip(new DateTime(ticks, kind));

        AssertSentinel(ticks, dateCreated);
        AssertSentinel(ticks, premiereDate);
        AssertSentinel(ticks, activityDateCreated);
    }

    [Theory]
    [InlineData(DateTimeKind.Local, DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local, DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified, DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Unspecified, DateTimeKind.Utc)]
    public void DateTime_MinValueOfOtherKind_IsFoundByEquality(DateTimeKind storedKind, DateTimeKind queriedKind)
    {
        var queried = new DateTime(DateTime.MinValue.Ticks, queriedKind);

        var found = StoreAndCountEqual(new DateTime(DateTime.MinValue.Ticks, storedKind), queried);

        Assert.Equal(1, found);
    }

    [Theory]
    [InlineData(DateTimeKind.Local, DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Local, DateTimeKind.Utc)]
    [InlineData(DateTimeKind.Unspecified, DateTimeKind.Unspecified)]
    [InlineData(DateTimeKind.Unspecified, DateTimeKind.Utc)]
    public void DateTime_MaxValueOfOtherKind_IsFoundByEquality(DateTimeKind storedKind, DateTimeKind queriedKind)
    {
        var queried = new DateTime(DateTime.MaxValue.Ticks, queriedKind);

        var found = StoreAndCountEqual(new DateTime(DateTime.MaxValue.Ticks, storedKind), queried);

        Assert.Equal(1, found);
    }

    [Fact]
    public void Model_DateTimeProperties_UseSharedUtcConverter()
    {
        using var context = CreateDbContext();

        var properties = context.Model.GetEntityTypes()
            .SelectMany(e => e.GetProperties())
            .Where(p => (Nullable.GetUnderlyingType(p.ClrType) ?? p.ClrType) == typeof(DateTime))
            .ToList();

        Assert.NotEmpty(properties);
        var isMySqlFamily = TestDatabaseEnvironment.Provider is TestDbProvider.MySql or TestDbProvider.MariaDb;
        Assert.All(
            properties,
            p =>
            {
                var converter = p.GetValueConverter();
                if (isMySqlFamily)
                {
                    // The column of these engines cannot hold the last tick of DateTime.MaxValue, so the converter also clamps it.
                    Assert.IsType<ClampedUtcDateTimeConverter>(converter);
                }
                else
                {
                    Assert.IsType<UtcDateTimeConverter>(converter);
                }
            });
    }

    private static void AssertUtc(DateTime expected, DateTime? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(DateTimeKind.Utc, actual.Value.Kind);
        Assert.Equal(expected, actual.Value, _microsecond);
    }

    private static void AssertSentinel(long expectedTicks, DateTime? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expectedTicks, actual.Value.Ticks);
        Assert.Equal(DateTimeKind.Utc, actual.Value.Kind);
    }

    private int StoreAndCountEqual(DateTime stored, DateTime queried)
    {
        var itemId = Guid.NewGuid();
        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = itemId,
                Type = typeof(Folder).FullName!,
                PremiereDate = stored
            });
            context.SaveChanges();
        }

        using (var context = CreateDbContext())
        {
            return context.BaseItems.Count(i => i.Id.Equals(itemId) && i.PremiereDate == queried);
        }
    }

    private (DateTime? DateCreated, DateTime? PremiereDate, DateTime ActivityDateCreated) RoundTrip(DateTime value)
    {
        var itemId = Guid.NewGuid();
        using (var context = CreateDbContext())
        {
            context.BaseItems.Add(new BaseItemEntity
            {
                Id = itemId,
                Type = typeof(Folder).FullName!,
                DateCreated = value,
                PremiereDate = value
            });
            context.ActivityLogs.Add(new ActivityLog("Test", "Test", Guid.Empty) { DateCreated = value });
            context.SaveChanges();
        }

        using (var context = CreateDbContext())
        {
            var item = context.BaseItems.Single(i => i.Id.Equals(itemId));
            var activity = context.ActivityLogs.Single();
            return (item.DateCreated, item.PremiereDate, activity.DateCreated);
        }
    }
}

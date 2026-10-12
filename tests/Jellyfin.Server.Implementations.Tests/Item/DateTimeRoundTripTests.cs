using System;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Item;

/// <summary>
/// The shared database context stores a <see cref="DateTime"/> as UTC whatever its <see cref="DateTimeKind"/>,
/// and SQLite reads it back as UTC. Values are compared to within a microsecond,
/// as the stored precision is not under test.
/// </summary>
public sealed class DateTimeRoundTripTests : SqliteDbTestFixture
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

    private static void AssertUtc(DateTime expected, DateTime? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(DateTimeKind.Utc, actual.Value.Kind);
        Assert.Equal(expected, actual.Value, _microsecond);
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

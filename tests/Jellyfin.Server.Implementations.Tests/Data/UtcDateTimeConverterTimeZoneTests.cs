using System;
using Jellyfin.Database.Implementations.ValueConverters;
using Jellyfin.Database.Testing;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

/// <summary>
/// Runs the converter with the process time zone set to a zone that is not UTC, so that the tests do not depend on
/// the zone of the machine. The zone of the machine is restored afterwards.
/// </summary>
[Collection(TimeZoneIsolation.Name)]
public class UtcDateTimeConverterTimeZoneTests
{
    private const long MinTicks = 0;

    private const long MaxTicks = 3155378975999999999;

    public static TheoryData<string, long, DateTimeKind> SentinelsInZones() => new()
    {
        { "America/New_York", MinTicks, DateTimeKind.Local },
        { "America/New_York", MinTicks, DateTimeKind.Unspecified },
        { "America/New_York", MaxTicks, DateTimeKind.Local },
        { "America/New_York", MaxTicks, DateTimeKind.Unspecified },
        { "Asia/Kolkata", MinTicks, DateTimeKind.Local },
        { "Asia/Kolkata", MinTicks, DateTimeKind.Unspecified },
        { "Asia/Kolkata", MaxTicks, DateTimeKind.Local },
        { "Asia/Kolkata", MaxTicks, DateTimeKind.Unspecified }
    };

    [Theory]
    [MemberData(nameof(SentinelsInZones))]
    public void ConvertToProvider_SentinelInZone_KeepsTicksAndIsUtc(string zoneId, long ticks, DateTimeKind kind)
    {
        using var zone = new TemporaryTimeZone(zoneId);

        var stored = Assert.IsType<DateTime>(new UtcDateTimeConverter().ConvertToProvider(new DateTime(ticks, kind)));

        Assert.Equal(ticks, stored.Ticks);
        Assert.Equal(DateTimeKind.Utc, stored.Kind);
    }

    [Theory]
    [InlineData("America/New_York", DateTimeKind.Local, 16, 34)]
    [InlineData("America/New_York", DateTimeKind.Unspecified, 16, 34)]
    [InlineData("Asia/Kolkata", DateTimeKind.Local, 7, 4)]
    [InlineData("Asia/Kolkata", DateTimeKind.Unspecified, 7, 4)]
    public void ConvertToProvider_OrdinaryValueInZone_IsConvertedFromServerTime(
        string zoneId,
        DateTimeKind kind,
        int expectedHour,
        int expectedMinute)
    {
        using var zone = new TemporaryTimeZone(zoneId);

        var stored = Assert.IsType<DateTime>(
            new UtcDateTimeConverter().ConvertToProvider(new DateTime(2021, 7, 15, 12, 34, 56, kind)));

        Assert.Equal(new DateTime(2021, 7, 15, expectedHour, expectedMinute, 56, DateTimeKind.Utc).Ticks, stored.Ticks);
        Assert.Equal(DateTimeKind.Utc, stored.Kind);
    }
}

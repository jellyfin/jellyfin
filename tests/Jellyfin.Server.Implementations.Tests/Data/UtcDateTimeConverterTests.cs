using System;
using Jellyfin.Database.Implementations.ValueConverters;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Data;

/// <summary>
/// The assertions compare literal ticks and kinds, so they hold in whichever time zone the host runs.
/// </summary>
public class UtcDateTimeConverterTests
{
    private const long MinTicks = 0;

    private const long MaxTicks = 3155378975999999999;

    public static TheoryData<long, DateTimeKind> SentinelsOfAnyKind() => new()
    {
        { MinTicks, DateTimeKind.Utc },
        { MinTicks, DateTimeKind.Local },
        { MinTicks, DateTimeKind.Unspecified },
        { MaxTicks, DateTimeKind.Utc },
        { MaxTicks, DateTimeKind.Local },
        { MaxTicks, DateTimeKind.Unspecified }
    };

    [Theory]
    [MemberData(nameof(SentinelsOfAnyKind))]
    public void ToStoredUtc_Sentinel_KeepsTicksAndIsUtc(long ticks, DateTimeKind kind)
    {
        var stored = UtcDateTimeConverter.ToStoredUtc(new DateTime(ticks, kind));

        Assert.Equal(ticks, stored.Ticks);
        Assert.Equal(DateTimeKind.Utc, stored.Kind);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(MaxTicks - 1)]
    public void ToStoredUtc_UtcValueNextToSentinel_IsUnchanged(long ticks)
    {
        var stored = UtcDateTimeConverter.ToStoredUtc(new DateTime(ticks, DateTimeKind.Utc));

        Assert.Equal(ticks, stored.Ticks);
        Assert.Equal(DateTimeKind.Utc, stored.Kind);
    }
}

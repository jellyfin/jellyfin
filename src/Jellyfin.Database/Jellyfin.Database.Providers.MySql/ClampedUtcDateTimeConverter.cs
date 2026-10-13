using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Jellyfin.Database.Providers.MySql;

/// <summary>
/// Writes a <see cref="DateTime"/> as UTC and reads it back with <see cref="DateTimeKind.Utc"/>, keeping
/// <see cref="DateTime.MaxValue"/> intact although the column holds microseconds and not ticks.
/// </summary>
/// <remarks>
/// A <c>datetime(6)</c> column rounds to the microsecond. <see cref="DateTime.MaxValue"/> ends in .9999999 seconds, which
/// rounds up to the year 10000, a value that neither server accepts. It is written as the last microsecond of the year
/// 9999 instead, and every value from there on is read as <see cref="DateTime.MaxValue"/>.
/// </remarks>
internal sealed class ClampedUtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    /// <summary>
    /// The last value a <c>datetime(6)</c> column holds, 9999-12-31 23:59:59.999999.
    /// </summary>
    internal static readonly DateTime LastStored = new(DateTime.MaxValue.Ticks - 9, DateTimeKind.Utc);

    /// <summary>
    /// Initializes a new instance of the <see cref="ClampedUtcDateTimeConverter"/> class.
    /// </summary>
    public ClampedUtcDateTimeConverter()
        : base(v => ToStore(v), v => FromStore(v))
    {
    }

    private static DateTime ToStore(DateTime value)
    {
        // DateTime.MinValue and DateTime.MaxValue are the "no value" markers and are not points in time of the host's zone.
        // Converting them as local time would move them by the offset of the host, so they are UTC whatever their kind.
        if (value.Ticks == DateTime.MinValue.Ticks)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        if (value.Ticks == DateTime.MaxValue.Ticks)
        {
            return LastStored;
        }

        var utc = value.ToUniversalTime();
        return utc >= LastStored ? LastStored : utc;
    }

    private static DateTime FromStore(DateTime value)
    {
        return value >= LastStored
            ? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
            : DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }
}

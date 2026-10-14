using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Jellyfin.Database.Implementations.ValueConverters;

/// <summary>
/// Writes a <see cref="DateTime"/> as UTC and reads it back with <see cref="DateTimeKind.Utc"/>.
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UtcDateTimeConverter"/> class.
    /// </summary>
    public UtcDateTimeConverter()
        : base(v => ToStoredUtc(v), v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }

    /// <summary>
    /// Gets the value to store for <paramref name="value"/>.
    /// <see cref="DateTime.MinValue"/> and <see cref="DateTime.MaxValue"/> keep their ticks and are labelled
    /// <see cref="DateTimeKind.Utc"/> whatever their kind, so the stored value does not depend on the time zone
    /// of the server. Any other value is converted with <see cref="DateTime.ToUniversalTime"/>.
    /// </summary>
    /// <param name="value">The value to store.</param>
    /// <returns>The value to store, with <see cref="DateTimeKind.Utc"/>.</returns>
    internal static DateTime ToStoredUtc(DateTime value)
    {
        if (value == DateTime.MinValue || value == DateTime.MaxValue)
        {
            return DateTime.SpecifyKind(value, DateTimeKind.Utc);
        }

        return value.ToUniversalTime();
    }
}

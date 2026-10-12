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
        : base(v => v.ToUniversalTime(), v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}

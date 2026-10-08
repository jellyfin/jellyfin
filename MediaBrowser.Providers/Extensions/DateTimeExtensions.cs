using System;

namespace MediaBrowser.Providers.Extensions;

/// <summary>
/// Extensions for normalizing dates returned by metadata providers.
/// </summary>
internal static class DateTimeExtensions
{
    /// <summary>
    /// Anchors a date-only metadata value to midnight UTC.
    /// </summary>
    /// <param name="value">The date reported by the provider.</param>
    /// <returns>The same calendar date, with <see cref="DateTimeKind.Utc"/>.</returns>
    public static DateTime AsCalendarDate(this DateTime value)
    {
        return DateTime.SpecifyKind(value, DateTimeKind.Utc);
    }

    /// <summary>
    /// Anchors an optional date-only metadata value to midnight UTC.
    /// </summary>
    /// <param name="value">The date reported by the provider, if any.</param>
    /// <returns>The same calendar date with <see cref="DateTimeKind.Utc"/>, or <c>null</c>.</returns>
    public static DateTime? AsCalendarDate(this DateTime? value)
    {
        return value.HasValue ? value.Value.AsCalendarDate() : null;
    }
}

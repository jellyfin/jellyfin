using System;

namespace Jellyfin.Extensions
{
    /// <summary>
    /// <see cref="DateTime"/> specific extensions.
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Gets midnight UTC on the calendar date of this value, without any time zone conversion.
        /// </summary>
        /// <remarks>
        /// For date-only metadata such as air dates and birthdays. <see cref="DateTime.ToUniversalTime"/> treats a
        /// date with no kind, or a local one, as local midnight, which moves it to the previous day whenever the
        /// server is ahead of UTC.
        /// </remarks>
        /// <param name="date">The date.</param>
        /// <returns>Midnight UTC on the same calendar date.</returns>
        public static DateTime ToUtcDate(this DateTime date)
            => DateTime.SpecifyKind(date.Date, DateTimeKind.Utc);
    }
}

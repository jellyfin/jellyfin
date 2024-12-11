#pragma warning disable CS1591

using System;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data
{
    /// <summary>
    /// Helper methods for working with <see cref="DynamicDayOfWeek"/>.
    /// </summary>
    public static class DayOfWeekHelper
    {
        /// <summary>
        /// Gets an array of <see cref="DayOfWeek"/> values corresponding to the specified <see cref="DynamicDayOfWeek"/>.
        /// </summary>
        /// <param name="day">The dynamic day of the week.</param>
        /// <returns>An array of <see cref="DayOfWeek"/> values.</returns>
        public static DayOfWeek[] GetDaysOfWeek(DynamicDayOfWeek day) =>
            day switch
            {
                DynamicDayOfWeek.Everyday => new[]
                {
                    DayOfWeek.Sunday,
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday,
                    DayOfWeek.Friday,
                    DayOfWeek.Saturday
                },
                DynamicDayOfWeek.Weekday => new[]
                {
                    DayOfWeek.Monday,
                    DayOfWeek.Tuesday,
                    DayOfWeek.Wednesday,
                    DayOfWeek.Thursday,
                    DayOfWeek.Friday
                },
                DynamicDayOfWeek.Weekend => new[]
                {
                    DayOfWeek.Sunday,
                    DayOfWeek.Saturday
                },
                _ => new[] { (DayOfWeek)day }
            };

        /// <summary>
        /// Determines whether the specified <see cref="DayOfWeek"/> is contained within the given <see cref="DynamicDayOfWeek"/>.
        /// </summary>
        /// <param name="dynamicDayOfWeek">The dynamic day of the week.</param>
        /// <param name="dayOfWeek">The specific day of the week to check.</param>
        /// <returns><c>true</c> if the day is contained; otherwise, <c>false</c>.</returns>
        public static bool Contains(this DynamicDayOfWeek dynamicDayOfWeek, DayOfWeek dayOfWeek) =>
            dynamicDayOfWeek switch
            {
                DynamicDayOfWeek.Everyday => true,
                DynamicDayOfWeek.Weekday => dayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday,
                DynamicDayOfWeek.Weekend => dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday,
                _ => (DayOfWeek)dynamicDayOfWeek == dayOfWeek
            };
    }
}

#pragma warning disable CS1591

using System;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data
{
    public static class DayOfWeekHelper
    {
        public static DayOfWeek[] GetDaysOfWeek(DynamicDayOfWeek day)
        {
            return day switch
            {
                DynamicDayOfWeek.Everyday => new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday, DayOfWeek.Sunday },
                DynamicDayOfWeek.Weekday => new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                DynamicDayOfWeek.Weekend => new[] { DayOfWeek.Saturday, DayOfWeek.Sunday },
                _ => new[] { (DayOfWeek)day }
            };
        }
    }
}

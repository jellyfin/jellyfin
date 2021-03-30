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
                DynamicDayOfWeek.Everyday => new[] { DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday },
                DynamicDayOfWeek.Weekday => new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday },
                DynamicDayOfWeek.Weekend => new[] { DayOfWeek.Sunday, DayOfWeek.Saturday },
                _ => new[] { (DayOfWeek)day }
            };
        }
    }
}

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using Jellyfin.Data.Enums;

namespace Jellyfin.Data
{
    public static class DayOfWeekHelper
    {
        public static List<DayOfWeek> GetDaysOfWeek(DynamicDayOfWeek day)
        {
            var days = new List<DayOfWeek>(7);

            if (day == DynamicDayOfWeek.Sunday
                || day == DynamicDayOfWeek.Weekend
                || day == DynamicDayOfWeek.Everyday)
            {
                days.Add(DayOfWeek.Sunday);
            }

            if (day == DynamicDayOfWeek.Monday
                || day == DynamicDayOfWeek.Weekday
                || day == DynamicDayOfWeek.Everyday)
            {
                days.Add(DayOfWeek.Monday);
            }

            if (day == DynamicDayOfWeek.Tuesday
                || day == DynamicDayOfWeek.Weekday
                || day == DynamicDayOfWeek.Everyday)
            {
                days.Add(DayOfWeek.Tuesday);
            }

            if (day == DynamicDayOfWeek.Wednesday
                || day == DynamicDayOfWeek.Weekday
                || day == DynamicDayOfWeek.Everyday)
            {
                days.Add(DayOfWeek.Wednesday);
            }

            if (day == DynamicDayOfWeek.Thursday
                || day == DynamicDayOfWeek.Weekday
                || day == DynamicDayOfWeek.Everyday)
            {
                days.Add(DayOfWeek.Thursday);
            }

            if (day == DynamicDayOfWeek.Friday
                || day == DynamicDayOfWeek.Weekday
                || day == DynamicDayOfWeek.Everyday)
            {
                days.Add(DayOfWeek.Friday);
            }

            if (day == DynamicDayOfWeek.Saturday
                || day == DynamicDayOfWeek.Weekend
                || day == DynamicDayOfWeek.Everyday)
            {
                days.Add(DayOfWeek.Saturday);
            }

            return days;
        }
    }
}

using MediaBrowser.Model.Configuration;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    public static class DayOfWeekHelper
    {
        public static List<DayOfWeek> GetDaysOfWeek(DynamicDayOfWeek day)
        {
            return GetDaysOfWeek(new List<DynamicDayOfWeek> { day });
        }

        public static List<DayOfWeek> GetDaysOfWeek(List<DynamicDayOfWeek> days)
        {
            var list = new List<DayOfWeek>();

            if (days.Contains(DynamicDayOfWeek.Sunday) ||
                days.Contains(DynamicDayOfWeek.Weekend) ||
                days.Contains(DynamicDayOfWeek.Everyday))
            {
                list.Add(DayOfWeek.Sunday);
            }

            if (days.Contains(DynamicDayOfWeek.Saturday) ||
                days.Contains(DynamicDayOfWeek.Weekend) ||
                days.Contains(DynamicDayOfWeek.Everyday))
            {
                list.Add(DayOfWeek.Saturday);
            }

            if (days.Contains(DynamicDayOfWeek.Monday) ||
                days.Contains(DynamicDayOfWeek.Weekday) ||
                days.Contains(DynamicDayOfWeek.Everyday))
            {
                list.Add(DayOfWeek.Monday);
            }

            if (days.Contains(DynamicDayOfWeek.Monday) ||
                days.Contains(DynamicDayOfWeek.Weekday) ||
                days.Contains(DynamicDayOfWeek.Everyday))
            {
                list.Add(DayOfWeek.Tuesday
                    );
            }

            if (days.Contains(DynamicDayOfWeek.Wednesday) ||
                days.Contains(DynamicDayOfWeek.Weekday) ||
                days.Contains(DynamicDayOfWeek.Everyday))
            {
                list.Add(DayOfWeek.Wednesday);
            }

            if (days.Contains(DynamicDayOfWeek.Thursday) ||
                days.Contains(DynamicDayOfWeek.Weekday) ||
                days.Contains(DynamicDayOfWeek.Everyday))
            {
                list.Add(DayOfWeek.Thursday);
            }

            if (days.Contains(DynamicDayOfWeek.Friday) ||
                days.Contains(DynamicDayOfWeek.Weekday) ||
                days.Contains(DynamicDayOfWeek.Everyday))
            {
                list.Add(DayOfWeek.Friday);
            }

            return list;
        }
    }
}

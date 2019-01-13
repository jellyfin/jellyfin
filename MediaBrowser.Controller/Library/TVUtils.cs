using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Class TVUtils
    /// </summary>
    public static class TVUtils
    {
        /// <summary>
        /// The TVDB API key
        /// </summary>
        public static readonly string TvdbApiKey = "OG4V3YJ3FAP7FP2K";
        public static readonly string TvdbBaseUrl = "https://www.thetvdb.com/";
        /// <summary>
        /// The banner URL
        /// </summary>
        public static readonly string BannerUrl = TvdbBaseUrl + "banners/";

        /// <summary>
        /// Gets the air days.
        /// </summary>
        /// <param name="day">The day.</param>
        /// <returns>List{DayOfWeek}.</returns>
        public static DayOfWeek[] GetAirDays(string day)
        {
            if (!string.IsNullOrEmpty(day))
            {
                if (string.Equals(day, "Daily", StringComparison.OrdinalIgnoreCase))
                {
                    return new DayOfWeek[]
                               {
                                   DayOfWeek.Sunday,
                                   DayOfWeek.Monday,
                                   DayOfWeek.Tuesday,
                                   DayOfWeek.Wednesday,
                                   DayOfWeek.Thursday,
                                   DayOfWeek.Friday,
                                   DayOfWeek.Saturday
                               };
                }

                DayOfWeek value;

                if (Enum.TryParse(day, true, out value))
                {
                    return new DayOfWeek[]
                               {
                                   value
                               };
                }

                return new DayOfWeek[]{};
            }
            return null;
        }
    }
}

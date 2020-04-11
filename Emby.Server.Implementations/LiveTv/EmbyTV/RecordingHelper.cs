#pragma warning disable CS1591

using System;
using System.Globalization;
using MediaBrowser.Controller.LiveTv;

namespace Emby.Server.Implementations.LiveTv.EmbyTV
{
    internal class RecordingHelper
    {
        public static DateTime GetStartTime(TimerInfo timer)
        {
            return timer.StartDate.AddSeconds(-timer.PrePaddingSeconds);
        }

        public static string GetRecordingName(TimerInfo info)
        {
            var name = info.Name;

            if (info.IsProgramSeries)
            {
                var addHyphen = true;

                if (info.SeasonNumber.HasValue && info.EpisodeNumber.HasValue)
                {
                    name += string.Format(
                        CultureInfo.InvariantCulture,
                        " S{0}E{1}",
                        info.SeasonNumber.Value.ToString("00", CultureInfo.InvariantCulture),
                        info.EpisodeNumber.Value.ToString("00", CultureInfo.InvariantCulture));
                    addHyphen = false;
                }
                else if (info.OriginalAirDate.HasValue)
                {
                    if (info.OriginalAirDate.Value.Date.Equals(info.StartDate.Date))
                    {
                        name += " " + GetDateString(info.StartDate);
                    }
                    else
                    {
                        name += " " + info.OriginalAirDate.Value.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    }
                }
                else
                {
                    name += " " + GetDateString(info.StartDate);
                }

                if (!string.IsNullOrWhiteSpace(info.EpisodeTitle))
                {
                    if (addHyphen)
                    {
                        name += " -";
                    }

                    name += " " + info.EpisodeTitle;
                }
            }

            else if (info.IsMovie && info.ProductionYear != null)
            {
                name += " (" + info.ProductionYear + ")";
            }
            else
            {
                name += " " + GetDateString(info.StartDate);
            }

            return name;
        }

        private static string GetDateString(DateTime date)
        {
            date = date.ToLocalTime();

            return string.Format(
                CultureInfo.InvariantCulture,
                "{0}_{1}_{2}_{3}_{4}_{5}",
                date.Year.ToString("0000", CultureInfo.InvariantCulture),
                date.Month.ToString("00", CultureInfo.InvariantCulture),
                date.Day.ToString("00", CultureInfo.InvariantCulture),
                date.Hour.ToString("00", CultureInfo.InvariantCulture),
                date.Minute.ToString("00", CultureInfo.InvariantCulture),
                date.Second.ToString("00", CultureInfo.InvariantCulture));
        }
    }
}

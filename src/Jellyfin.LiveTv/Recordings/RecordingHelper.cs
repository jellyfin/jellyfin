using System;
using System.Globalization;
using System.Text;
using MediaBrowser.Controller.LiveTv;

namespace Jellyfin.LiveTv.Recordings
{
    internal static class RecordingHelper
    {
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
                    var tmpName = name;
                    if (addHyphen)
                    {
                        tmpName += " -";
                    }

                    tmpName += " " + info.EpisodeTitle;
                    // Since the filename will be used with file ext. (.mp4, .ts, etc)
                    if (Encoding.UTF8.GetByteCount(tmpName) < 250)
                    {
                        name = tmpName;
                    }
                }
            }
            else if (info.IsMovie && info.ProductionYear is not null)
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
            return date.ToLocalTime().ToString("yyyy_MM_dd_HH_mm_ss", CultureInfo.InvariantCulture);
        }
    }
}

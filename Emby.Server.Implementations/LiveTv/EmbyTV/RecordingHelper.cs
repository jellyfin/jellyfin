using MediaBrowser.Controller.LiveTv;
using System;
using System.Globalization;

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
                    name += string.Format(" S{0}E{1}", info.SeasonNumber.Value.ToString("00", CultureInfo.InvariantCulture), info.EpisodeNumber.Value.ToString("00", CultureInfo.InvariantCulture));
                    addHyphen = false;
                }
                else if (info.OriginalAirDate.HasValue)
                {
                    name += " " + info.OriginalAirDate.Value.ToString("yyyy-MM-dd");
                }
                else
                {
                    name += " " + DateTime.Now.ToString("yyyy-MM-dd");
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
                name += " " + info.StartDate.ToString("yyyy-MM-dd");
            }

            return name;
        }
    }
}

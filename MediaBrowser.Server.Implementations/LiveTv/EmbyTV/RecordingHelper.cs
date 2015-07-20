using System.Text;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Server.Implementations.LiveTv.EmbyTV
{
    internal class RecordingHelper
    {
        public static List<TimerInfo> GetTimersForSeries(SeriesTimerInfo seriesTimer, IEnumerable<ProgramInfo> epgData, IReadOnlyList<RecordingInfo> currentRecordings, ILogger logger)
        {
            List<TimerInfo> timers = new List<TimerInfo>();

            // Filtered Per Show
            var filteredEpg = epgData.Where(epg => epg.Id.Substring(0, 10) == seriesTimer.Id);

            if (!seriesTimer.RecordAnyTime)
            {
                filteredEpg = filteredEpg.Where(epg => (seriesTimer.StartDate.TimeOfDay == epg.StartDate.TimeOfDay));
            }

            if (seriesTimer.RecordNewOnly)
            {
                filteredEpg = filteredEpg.Where(epg => !epg.IsRepeat); //Filtered by New only
            }

            if (!seriesTimer.RecordAnyChannel)
            {
                filteredEpg = filteredEpg.Where(epg => string.Equals(epg.ChannelId, seriesTimer.ChannelId, StringComparison.OrdinalIgnoreCase));
            }

            filteredEpg = filteredEpg.Where(epg => seriesTimer.Days.Contains(epg.StartDate.DayOfWeek));

            filteredEpg = filteredEpg.Where(epg => currentRecordings.All(r => r.Id.Substring(0, 14) != epg.Id.Substring(0, 14))); //filtered recordings already running

            filteredEpg = filteredEpg.GroupBy(epg => epg.Id.Substring(0, 14)).Select(g => g.First()).ToList();

            foreach (var epg in filteredEpg)
            {
                timers.Add(CreateTimer(epg, seriesTimer));
            }

            return timers;
        }

        public static DateTime GetStartTime(TimerInfo timer)
        {
            if (timer.StartDate.AddSeconds(-timer.PrePaddingSeconds + 1) < DateTime.UtcNow)
            {
                return DateTime.UtcNow.AddSeconds(1);
            }
            return timer.StartDate.AddSeconds(-timer.PrePaddingSeconds);
        }

        public static TimerInfo CreateTimer(ProgramInfo parent, SeriesTimerInfo series)
        {
            var timer = new TimerInfo();

            timer.ChannelId = parent.ChannelId;
            timer.Id = (series.Id + parent.Id).GetMD5().ToString("N");
            timer.StartDate = parent.StartDate;
            timer.EndDate = parent.EndDate;
            timer.ProgramId = parent.Id;
            timer.PrePaddingSeconds = series.PrePaddingSeconds;
            timer.PostPaddingSeconds = series.PostPaddingSeconds;
            timer.IsPostPaddingRequired = series.IsPostPaddingRequired;
            timer.IsPrePaddingRequired = series.IsPrePaddingRequired;
            timer.Priority = series.Priority;
            timer.Name = parent.Name;
            timer.Overview = parent.Overview;
            timer.SeriesTimerId = series.Id;

            return timer;
        }

        public static string GetRecordingName(TimerInfo timer, ProgramInfo info)
        {
            if (info == null)
            {
                return (timer.ProgramId + ".ts");
            }
            var fancyName = info.Name;
            if (info.ProductionYear != null)
            {
                fancyName += "_(" + info.ProductionYear + ")";
            }
            if (info.IsSeries)
            {
                fancyName += "_" + info.EpisodeTitle.Replace("Season: ", "S").Replace(" Episode: ", "E");
            }
            if (info.IsHD ?? false)
            {
                fancyName += "_HD";
            }
            if (info.OriginalAirDate != null)
            {
                fancyName += "_" + info.OriginalAirDate.Value.ToString("yyyy-MM-dd");
            }
            return RemoveSpecialCharacters(fancyName) + ".ts";
        }

        public static string RemoveSpecialCharacters(string str)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in str)
            {
                if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '.' || c == '_' || c == '-' || c == ' ')
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }
    }
}

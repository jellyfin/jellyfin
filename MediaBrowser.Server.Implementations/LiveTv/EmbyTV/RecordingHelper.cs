using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.LiveTv;
using System;
using System.Text;

namespace MediaBrowser.Server.Implementations.LiveTv.EmbyTV
{
    internal class RecordingHelper
    {
        public static DateTime GetStartTime(TimerInfo timer)
        {
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

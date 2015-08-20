using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.LiveTv;
using System;
using System.Globalization;

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
                return timer.ProgramId + ".ts";
            }

            var name = info.Name;

            if (info.IsSeries)
            {
                if (info.SeasonNumber.HasValue && info.EpisodeNumber.HasValue)
                {
                    name += string.Format(" S{0}E{1}", info.SeasonNumber.Value.ToString("00", CultureInfo.InvariantCulture), info.EpisodeNumber.Value.ToString("00", CultureInfo.InvariantCulture));
                }
                else if (info.OriginalAirDate.HasValue)
                {
                    name += " " + info.OriginalAirDate.Value.ToString("yyyy-MM-dd");
                }
                else if (!string.IsNullOrWhiteSpace(info.EpisodeTitle))
                {
                    name += " " + info.EpisodeTitle;
                }
            }

            else if (info.ProductionYear != null)
            {
                name += " (" + info.ProductionYear + ")";
            }

            return name + ".ts";
        }
    }
}

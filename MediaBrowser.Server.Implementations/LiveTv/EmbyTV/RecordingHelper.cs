using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.LiveTv;
using System;
using System.Globalization;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Server.Implementations.LiveTv.EmbyTV
{
    internal class RecordingHelper
    {
        public static DateTime GetStartTime(TimerInfo timer)
        {
            return timer.StartDate.AddSeconds(-timer.PrePaddingSeconds);
        }

        public static TimerInfo CreateTimer(ProgramInfo parent, SeriesTimerInfo seriesTimer)
        {
            var timer = new TimerInfo();

            timer.ChannelId = parent.ChannelId;
            timer.Id = (seriesTimer.Id + parent.Id).GetMD5().ToString("N");
            timer.StartDate = parent.StartDate;
            timer.EndDate = parent.EndDate;
            timer.ProgramId = parent.Id;
            timer.PrePaddingSeconds = seriesTimer.PrePaddingSeconds;
            timer.PostPaddingSeconds = seriesTimer.PostPaddingSeconds;
            timer.IsPostPaddingRequired = seriesTimer.IsPostPaddingRequired;
            timer.IsPrePaddingRequired = seriesTimer.IsPrePaddingRequired;
            timer.KeepUntil = seriesTimer.KeepUntil;
            timer.Priority = seriesTimer.Priority;
            timer.Name = parent.Name;
            timer.Overview = parent.Overview;
            timer.SeriesTimerId = seriesTimer.Id;

            CopyProgramInfoToTimerInfo(parent, timer);

            return timer;
        }

        public static void CopyProgramInfoToTimerInfo(ProgramInfo programInfo, TimerInfo timerInfo)
        {
            timerInfo.SeasonNumber = programInfo.SeasonNumber;
            timerInfo.EpisodeNumber = programInfo.EpisodeNumber;
            timerInfo.IsMovie = programInfo.IsMovie;
            timerInfo.IsKids = programInfo.IsKids;
            timerInfo.IsNews = programInfo.IsNews;
            timerInfo.IsSports = programInfo.IsSports;
            timerInfo.ProductionYear = programInfo.ProductionYear;
            timerInfo.EpisodeTitle = programInfo.EpisodeTitle;
            timerInfo.OriginalAirDate = programInfo.OriginalAirDate;
            timerInfo.IsProgramSeries = programInfo.IsSeries;

            timerInfo.HomePageUrl = programInfo.HomePageUrl;
            timerInfo.CommunityRating = programInfo.CommunityRating;
            timerInfo.ShortOverview = programInfo.ShortOverview;
            timerInfo.OfficialRating = programInfo.OfficialRating;
            timerInfo.IsRepeat = programInfo.IsRepeat;
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
                name += " " + info.StartDate.ToString("yyyy-MM-dd") + " " + info.Id;
            }

            return name;
        }
    }
}

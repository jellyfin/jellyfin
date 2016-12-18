using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.LiveTv;
using System;
using System.Globalization;
using MediaBrowser.Model.LiveTv;

namespace Emby.Server.Implementations.LiveTv.EmbyTV
{
    internal class RecordingHelper
    {
        public static DateTime GetStartTime(TimerInfo timer)
        {
            return timer.StartDate.AddSeconds(-timer.PrePaddingSeconds);
        }

        public static TimerInfo CreateTimer(ProgramInfo parent, SeriesTimerInfo seriesTimer)
        {
            var timer = new TimerInfo
            {
                ChannelId = parent.ChannelId,
                Id = (seriesTimer.Id + parent.Id).GetMD5().ToString("N"),
                StartDate = parent.StartDate,
                EndDate = parent.EndDate,
                ProgramId = parent.Id,
                PrePaddingSeconds = seriesTimer.PrePaddingSeconds,
                PostPaddingSeconds = seriesTimer.PostPaddingSeconds,
                IsPostPaddingRequired = seriesTimer.IsPostPaddingRequired,
                IsPrePaddingRequired = seriesTimer.IsPrePaddingRequired,
                KeepUntil = seriesTimer.KeepUntil,
                Priority = seriesTimer.Priority,
                Name = parent.Name,
                Overview = parent.Overview,
                SeriesId = parent.SeriesId,
                SeriesTimerId = seriesTimer.Id,
                ShowId = parent.ShowId
            };

            CopyProgramInfoToTimerInfo(parent, timer);

            return timer;
        }

        public static void CopyProgramInfoToTimerInfo(ProgramInfo programInfo, TimerInfo timerInfo)
        {
            timerInfo.Name = programInfo.Name;
            timerInfo.StartDate = programInfo.StartDate;
            timerInfo.EndDate = programInfo.EndDate;
            timerInfo.ChannelId = programInfo.ChannelId;

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
            timerInfo.Overview = programInfo.Overview;
            timerInfo.ShortOverview = programInfo.ShortOverview;
            timerInfo.OfficialRating = programInfo.OfficialRating;
            timerInfo.IsRepeat = programInfo.IsRepeat;
            timerInfo.SeriesId = programInfo.SeriesId;
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

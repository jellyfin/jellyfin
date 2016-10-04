using MediaBrowser.Model.LiveTv;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.LiveTv
{
    public class TimerInfo
    {
        public TimerInfo()
        {
            Genres = new List<string>();
            KeepUntil = KeepUntil.UntilDeleted;
        }

        /// <summary>
        /// Id of the recording.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the series timer identifier.
        /// </summary>
        /// <value>The series timer identifier.</value>
        public string SeriesTimerId { get; set; }

        /// <summary>
        /// ChannelId of the recording.
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the program identifier.
        /// </summary>
        /// <value>The program identifier.</value>
        public string ProgramId { get; set; }

        /// <summary>
        /// Name of the recording.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Description of the recording.
        /// </summary>
        public string Overview { get; set; }

        /// <summary>
        /// The start date of the recording, in UTC.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// The end date of the recording, in UTC.
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        /// <value>The status.</value>
        public RecordingStatus Status { get; set; }

        /// <summary>
        /// Gets or sets the pre padding seconds.
        /// </summary>
        /// <value>The pre padding seconds.</value>
        public int PrePaddingSeconds { get; set; }

        /// <summary>
        /// Gets or sets the post padding seconds.
        /// </summary>
        /// <value>The post padding seconds.</value>
        public int PostPaddingSeconds { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is pre padding required.
        /// </summary>
        /// <value><c>true</c> if this instance is pre padding required; otherwise, <c>false</c>.</value>
        public bool IsPrePaddingRequired { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is post padding required.
        /// </summary>
        /// <value><c>true</c> if this instance is post padding required; otherwise, <c>false</c>.</value>
        public bool IsPostPaddingRequired { get; set; }

        /// <summary>
        /// Gets or sets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public int Priority { get; set; }


        // Program properties
        public int? SeasonNumber { get; set; }
        /// <summary>
        /// Gets or sets the episode number.
        /// </summary>
        /// <value>The episode number.</value>
        public int? EpisodeNumber { get; set; }
        public bool IsMovie { get; set; }
        public bool IsKids { get; set; }
        public bool IsSports { get; set; }
        public bool IsNews { get; set; }
        public int? ProductionYear { get; set; }
        public string EpisodeTitle { get; set; }
        public DateTime? OriginalAirDate { get; set; }
        public bool IsProgramSeries { get; set; }
        public bool IsRepeat { get; set; }
        public string HomePageUrl { get; set; }
        public float? CommunityRating { get; set; }
        public string ShortOverview { get; set; }
        public string OfficialRating { get; set; }
        public List<string> Genres { get; set; }
        public string RecordingPath { get; set; }
        public KeepUntil KeepUntil { get; set; }
    }
}

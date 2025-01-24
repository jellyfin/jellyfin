#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Jellyfin.Extensions;
using MediaBrowser.Model.LiveTv;

namespace MediaBrowser.Controller.LiveTv
{
    public class TimerInfo
    {
        public TimerInfo()
        {
            Genres = Array.Empty<string>();
            KeepUntil = KeepUntil.UntilDeleted;
            ProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            SeriesProviderIds = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Tags = Array.Empty<string>();
        }

        public Dictionary<string, string> ProviderIds { get; set; }

        public Dictionary<string, string> SeriesProviderIds { get; set; }

        public IReadOnlyList<string> Tags { get; set; }

        /// <summary>
        /// Gets or sets the id of the recording.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the series timer identifier.
        /// </summary>
        public string SeriesTimerId { get; set; }

        /// <summary>
        /// Gets or sets the channelId of the recording.
        /// </summary>
        public string ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the program identifier.
        /// </summary>
        /// <value>The program identifier.</value>
        public string ProgramId { get; set; }

        public string ShowId { get; set; }

        /// <summary>
        /// Gets or sets the name of the recording.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the description of the recording.
        /// </summary>
        public string Overview { get; set; }

        public string SeriesId { get; set; }

        /// <summary>
        /// Gets or sets the start date of the recording, in UTC.
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date of the recording, in UTC.
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

        public bool IsManual { get; set; }

        /// <summary>
        /// Gets or sets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public int Priority { get; set; }

        public int RetryCount { get; set; }

        // Program properties
        public int? SeasonNumber { get; set; }

        /// <summary>
        /// Gets or sets the episode number.
        /// </summary>
        /// <value>The episode number.</value>
        public int? EpisodeNumber { get; set; }

        public bool IsMovie { get; set; }

        public bool IsKids => Tags.Contains("Kids", StringComparison.OrdinalIgnoreCase);

        public bool IsSports => Tags.Contains("Sports", StringComparison.OrdinalIgnoreCase);

        public bool IsNews => Tags.Contains("News", StringComparison.OrdinalIgnoreCase);

        public bool IsSeries { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is live.
        /// </summary>
        /// <value><c>true</c> if this instance is live; otherwise, <c>false</c>.</value>
        [JsonIgnore]
        public bool IsLive => Tags.Contains("Live", StringComparison.OrdinalIgnoreCase);

        [JsonIgnore]
        public bool IsPremiere => Tags.Contains("Premiere", StringComparison.OrdinalIgnoreCase);

        public int? ProductionYear { get; set; }

        public string EpisodeTitle { get; set; }

        public DateTime? OriginalAirDate { get; set; }

        public bool IsProgramSeries { get; set; }

        public bool IsRepeat { get; set; }

        public string HomePageUrl { get; set; }

        public float? CommunityRating { get; set; }

        public string OfficialRating { get; set; }

        public string[] Genres { get; set; }

        public string RecordingPath { get; set; }

        public KeepUntil KeepUntil { get; set; }
    }
}

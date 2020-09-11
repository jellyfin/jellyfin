#nullable disable
#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.LiveTv
{
    /// <summary>
    /// Class SeriesTimerInfoDto.
    /// </summary>
    public class SeriesTimerInfoDto : BaseTimerInfoDto
    {
        public SeriesTimerInfoDto()
        {
            ImageTags = new Dictionary<ImageType, string>();
            Days = Array.Empty<DayOfWeek>();
            Type = "SeriesTimer";
        }

        /// <summary>
        /// Gets or sets a value indicating whether [record any time].
        /// </summary>
        /// <value><c>true</c> if [record any time]; otherwise, <c>false</c>.</value>
        public bool RecordAnyTime { get; set; }

        public bool SkipEpisodesInLibrary { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [record any channel].
        /// </summary>
        /// <value><c>true</c> if [record any channel]; otherwise, <c>false</c>.</value>
        public bool RecordAnyChannel { get; set; }

        public int KeepUpTo { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [record new only].
        /// </summary>
        /// <value><c>true</c> if [record new only]; otherwise, <c>false</c>.</value>
        public bool RecordNewOnly { get; set; }

        /// <summary>
        /// Gets or sets the days.
        /// </summary>
        /// <value>The days.</value>
        public DayOfWeek[] Days { get; set; }

        /// <summary>
        /// Gets or sets the day pattern.
        /// </summary>
        /// <value>The day pattern.</value>
        public DayPattern? DayPattern { get; set; }

        /// <summary>
        /// Gets or sets the image tags.
        /// </summary>
        /// <value>The image tags.</value>
        public Dictionary<ImageType, string> ImageTags { get; set; }

        /// <summary>
        /// Gets or sets the parent thumb item id.
        /// </summary>
        /// <value>The parent thumb item id.</value>
        public string ParentThumbItemId { get; set; }

        /// <summary>
        /// Gets or sets the parent thumb image tag.
        /// </summary>
        /// <value>The parent thumb image tag.</value>
        public string ParentThumbImageTag { get; set; }

        /// <summary>
        /// Gets or sets the parent primary image item identifier.
        /// </summary>
        /// <value>The parent primary image item identifier.</value>
        public string ParentPrimaryImageItemId { get; set; }

        /// <summary>
        /// Gets or sets the parent primary image tag.
        /// </summary>
        /// <value>The parent primary image tag.</value>
        public string ParentPrimaryImageTag { get; set; }
    }

    public enum KeepUntil
    {
        UntilDeleted,
        UntilSpaceNeeded,
        UntilWatched,
        UntilDate
    }
}

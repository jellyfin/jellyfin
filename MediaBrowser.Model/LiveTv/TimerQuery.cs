#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.LiveTv
{
    public class TimerQuery
    {
        /// <summary>
        /// Gets or sets the channel identifier.
        /// </summary>
        /// <value>The channel identifier.</value>
        public string ChannelId { get; set; }

        /// <summary>
        /// Gets or sets the selected Live TV view identifier.
        /// </summary>
        public Guid? ParentId { get; set; }

        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the series timer identifier.
        /// </summary>
        /// <value>The series timer identifier.</value>
        public string SeriesTimerId { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsScheduled { get; set; }
    }
}

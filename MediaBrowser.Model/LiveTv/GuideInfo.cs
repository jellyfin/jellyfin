#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.LiveTv
{
    public class GuideInfo
    {
        /// <summary>
        /// Gets or sets the start date.
        /// </summary>
        /// <value>The start date.</value>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// Gets or sets the end date.
        /// </summary>
        /// <value>The end date.</value>
        public DateTime EndDate { get; set; }
    }
}

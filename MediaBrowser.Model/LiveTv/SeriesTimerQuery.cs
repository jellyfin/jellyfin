#pragma warning disable CS1591

using MediaBrowser.Model.Entities;

namespace MediaBrowser.Model.LiveTv
{
    public class SeriesTimerQuery
    {
        /// <summary>
        /// Gets or sets the sort by - SortName, Priority
        /// </summary>
        /// <value>The sort by.</value>
        public string? SortBy { get; set; }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        /// <value>The sort order.</value>
        public SortOrder SortOrder { get; set; }
    }
}

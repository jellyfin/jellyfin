using Jellyfin.Model.Entities;

namespace Jellyfin.Model.LiveTv
{
    public class SeriesTimerQuery
    {
        /// <summary>
        /// Gets or sets the sort by - SortName, Priority
        /// </summary>
        /// <value>The sort by.</value>
        public string SortBy { get; set; }

        /// <summary>
        /// Gets or sets the sort order.
        /// </summary>
        /// <value>The sort order.</value>
        public SortOrder SortOrder { get; set; }
    }
}

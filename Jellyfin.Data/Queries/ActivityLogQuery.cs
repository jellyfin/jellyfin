using System;

namespace Jellyfin.Data.Queries
{
    /// <summary>
    /// A class representing a query to the activity logs.
    /// </summary>
    public class ActivityLogQuery : PaginatedQuery
    {
        /// <summary>
        /// Gets or sets a value indicating whether to take entries with a user id.
        /// </summary>
        public bool? HasUserId { get; set; }

        /// <summary>
        /// Gets or sets the minimum date to query for.
        /// </summary>
        public DateTime? MinDate { get; set; }
    }
}

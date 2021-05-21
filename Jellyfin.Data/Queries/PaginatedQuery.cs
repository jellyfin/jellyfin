namespace Jellyfin.Data.Queries
{
    /// <summary>
    /// An abstract class for paginated queries.
    /// </summary>
    public abstract class PaginatedQuery
    {
        /// <summary>
        /// Gets or sets the index to start at.
        /// </summary>
        public int? Skip { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of items to include.
        /// </summary>
        public int? Limit { get; set; }
    }
}

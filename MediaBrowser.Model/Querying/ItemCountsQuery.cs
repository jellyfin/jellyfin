namespace MediaBrowser.Model.Querying
{
    /// <summary>
    /// Class ItemCountsQuery.
    /// </summary>
    public class ItemCountsQuery
    {
        /// <summary>
        /// Gets or sets the user id.
        /// </summary>
        /// <value>The user id.</value>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is favorite.
        /// </summary>
        /// <value><c>null</c> if [is favorite] contains no value, <c>true</c> if [is favorite]; otherwise, <c>false</c>.</value>
        public bool? IsFavorite { get; set; }
    }
}

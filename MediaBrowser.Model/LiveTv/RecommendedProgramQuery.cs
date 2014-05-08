namespace MediaBrowser.Model.LiveTv
{
    public class RecommendedProgramQuery
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is airing.
        /// </summary>
        /// <value><c>true</c> if this instance is airing; otherwise, <c>false</c>.</value>
        public bool? IsAiring { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance has aired.
        /// </summary>
        /// <value><c>null</c> if [has aired] contains no value, <c>true</c> if [has aired]; otherwise, <c>false</c>.</value>
        public bool? HasAired { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        public int? Limit { get; set; }
    }
}
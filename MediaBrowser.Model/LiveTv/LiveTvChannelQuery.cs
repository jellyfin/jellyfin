
namespace MediaBrowser.Model.LiveTv
{
    /// <summary>
    /// Class ChannelQuery.
    /// </summary>
    public class LiveTvChannelQuery
    {
        /// <summary>
        /// Gets or sets the type of the channel.
        /// </summary>
        /// <value>The type of the channel.</value>
        public ChannelType? ChannelType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is favorite.
        /// </summary>
        /// <value><c>null</c> if [is favorite] contains no value, <c>true</c> if [is favorite]; otherwise, <c>false</c>.</value>
        public bool? IsFavorite { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is liked.
        /// </summary>
        /// <value><c>null</c> if [is liked] contains no value, <c>true</c> if [is liked]; otherwise, <c>false</c>.</value>
        public bool? IsLiked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is disliked.
        /// </summary>
        /// <value><c>null</c> if [is disliked] contains no value, <c>true</c> if [is disliked]; otherwise, <c>false</c>.</value>
        public bool? IsDisliked { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [enable favorite sorting].
        /// </summary>
        /// <value><c>true</c> if [enable favorite sorting]; otherwise, <c>false</c>.</value>
        public bool EnableFavoriteSorting { get; set; }
        
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }

        /// <summary>
        /// Skips over a given number of items within the results. Use for paging.
        /// </summary>
        /// <value>The start index.</value>
        public int? StartIndex { get; set; }

        /// <summary>
        /// The maximum number of items to return
        /// </summary>
        /// <value>The limit.</value>
        public int? Limit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [add current program].
        /// </summary>
        /// <value><c>true</c> if [add current program]; otherwise, <c>false</c>.</value>
        public bool AddCurrentProgram { get; set; }
        public bool EnableUserData { get; set; }

        public LiveTvChannelQuery()
        {
            EnableUserData = true;
        }
    }
}

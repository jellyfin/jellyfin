
namespace MediaBrowser.Model.Providers
{
    /// <summary>
    /// Class RemoteImageInfo
    /// </summary>
    public class RemoteImageInfo
    {
        /// <summary>
        /// Gets or sets the name of the provider.
        /// </summary>
        /// <value>The name of the provider.</value>
        public string ProviderName { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        public int? Height { get; set; }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        public int? Width { get; set; }

        /// <summary>
        /// Gets or sets the community rating.
        /// </summary>
        /// <value>The community rating.</value>
        public double? CommunityRating { get; set; }

        /// <summary>
        /// Gets or sets the vote count.
        /// </summary>
        /// <value>The vote count.</value>
        public int? VoteCount { get; set; }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <value>The language.</value>
        public string Language { get; set; }
    }
}

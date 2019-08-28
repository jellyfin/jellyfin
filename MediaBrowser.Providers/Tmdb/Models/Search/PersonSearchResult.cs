namespace MediaBrowser.Providers.Tmdb.Models.Search
{
    public class PersonSearchResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="PersonSearchResult" /> is adult.
        /// </summary>
        /// <value><c>true</c> if adult; otherwise, <c>false</c>.</value>
        public bool Adult { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the profile_ path.
        /// </summary>
        /// <value>The profile_ path.</value>
        public string Profile_Path { get; set; }
    }
}

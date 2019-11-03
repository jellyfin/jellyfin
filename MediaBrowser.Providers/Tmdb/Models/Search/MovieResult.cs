namespace MediaBrowser.Providers.Tmdb.Models.Search
{
    public class MovieResult
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="MovieResult" /> is adult.
        /// </summary>
        /// <value><c>true</c> if adult; otherwise, <c>false</c>.</value>
        public bool Adult { get; set; }
        /// <summary>
        /// Gets or sets the backdrop_path.
        /// </summary>
        /// <value>The backdrop_path.</value>
        public string Backdrop_Path { get; set; }
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the original_title.
        /// </summary>
        /// <value>The original_title.</value>
        public string Original_Title { get; set; }
        /// <summary>
        /// Gets or sets the original_name.
        /// </summary>
        /// <value>The original_name.</value>
        public string Original_Name { get; set; }
        /// <summary>
        /// Gets or sets the release_date.
        /// </summary>
        /// <value>The release_date.</value>
        public string Release_Date { get; set; }
        /// <summary>
        /// Gets or sets the poster_path.
        /// </summary>
        /// <value>The poster_path.</value>
        public string Poster_Path { get; set; }
        /// <summary>
        /// Gets or sets the popularity.
        /// </summary>
        /// <value>The popularity.</value>
        public double Popularity { get; set; }
        /// <summary>
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        public string Title { get; set; }
        /// <summary>
        /// Gets or sets the vote_average.
        /// </summary>
        /// <value>The vote_average.</value>
        public double Vote_Average { get; set; }
        /// <summary>
        /// For collection search results
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the vote_count.
        /// </summary>
        /// <value>The vote_count.</value>
        public int Vote_Count { get; set; }
    }
}

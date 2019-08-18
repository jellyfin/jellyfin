namespace MediaBrowser.Providers.Tmdb.Models.Search
{
    public class MovieResult
    {
            /// <summary>
            /// Gets or sets a value indicating whether this <see cref="TmdbMovieSearchResult" /> is adult.
            /// </summary>
            /// <value><c>true</c> if adult; otherwise, <c>false</c>.</value>
            public bool adult { get; set; }
            /// <summary>
            /// Gets or sets the backdrop_path.
            /// </summary>
            /// <value>The backdrop_path.</value>
            public string backdrop_path { get; set; }
            /// <summary>
            /// Gets or sets the id.
            /// </summary>
            /// <value>The id.</value>
            public int id { get; set; }
            /// <summary>
            /// Gets or sets the original_title.
            /// </summary>
            /// <value>The original_title.</value>
            public string original_title { get; set; }
            /// <summary>
            /// Gets or sets the original_name.
            /// </summary>
            /// <value>The original_name.</value>
            public string original_name { get; set; }
            /// <summary>
            /// Gets or sets the release_date.
            /// </summary>
            /// <value>The release_date.</value>
            public string release_date { get; set; }
            /// <summary>
            /// Gets or sets the poster_path.
            /// </summary>
            /// <value>The poster_path.</value>
            public string poster_path { get; set; }
            /// <summary>
            /// Gets or sets the popularity.
            /// </summary>
            /// <value>The popularity.</value>
            public double popularity { get; set; }
            /// <summary>
            /// Gets or sets the title.
            /// </summary>
            /// <value>The title.</value>
            public string title { get; set; }
            /// <summary>
            /// Gets or sets the vote_average.
            /// </summary>
            /// <value>The vote_average.</value>
            public double vote_average { get; set; }
            /// <summary>
            /// For collection search results
            /// </summary>
            public string name { get; set; }
            /// <summary>
            /// Gets or sets the vote_count.
            /// </summary>
            /// <value>The vote_count.</value>
            public int vote_count { get; set; }
    }
}

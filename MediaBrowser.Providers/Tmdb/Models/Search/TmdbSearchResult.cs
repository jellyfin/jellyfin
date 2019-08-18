using System.Collections.Generic;

namespace MediaBrowser.Providers.Tmdb.Models.Search
{
    public class TmdbSearchResult<T>
    {
        /// <summary>
        /// Gets or sets the page.
        /// </summary>
        /// <value>The page.</value>
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the results.
        /// </summary>
        /// <value>The results.</value>
        public List<T> Results { get; set; }

        /// <summary>
        /// Gets or sets the total_pages.
        /// </summary>
        /// <value>The total_pages.</value>
        public int Total_Pages { get; set; }

        /// <summary>
        /// Gets or sets the total_results.
        /// </summary>
        /// <value>The total_results.</value>
        public int Total_Results { get; set; }
    }
}

using System.Collections.Generic;

namespace MediaBrowser.Providers.Tmdb.Models.Search
{
    public class TvSearchResults
    {
        /// <summary>
        /// Gets or sets the page.
        /// </summary>
        /// <value>The page.</value>
        public int page { get; set; }
        /// <summary>
        /// Gets or sets the results.
        /// </summary>
        /// <value>The results.</value>
        public List<TvResult> results { get; set; }
        /// <summary>
        /// Gets or sets the total_pages.
        /// </summary>
        /// <value>The total_pages.</value>
        public int total_pages { get; set; }
        /// <summary>
        /// Gets or sets the total_results.
        /// </summary>
        /// <value>The total_results.</value>
        public int total_results { get; set; }
    }
}

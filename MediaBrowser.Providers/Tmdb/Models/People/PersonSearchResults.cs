using System.Collections.Generic;

namespace MediaBrowser.Providers.Tmdb.Models.People
{
    public class PersonSearchResults
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
        public List<PersonSearchResult> Results { get; set; }
        /// <summary>
        /// Gets or sets the total_ pages.
        /// </summary>
        /// <value>The total_ pages.</value>
        public int Total_Pages { get; set; }
        /// <summary>
        /// Gets or sets the total_ results.
        /// </summary>
        /// <value>The total_ results.</value>
        public int Total_Results { get; set; }
    }
}

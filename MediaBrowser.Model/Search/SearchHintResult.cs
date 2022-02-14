using System.Collections.Generic;

namespace MediaBrowser.Model.Search
{
    /// <summary>
    /// Class SearchHintResult.
    /// </summary>
    public class SearchHintResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SearchHintResult" /> class.
        /// </summary>
        /// <param name="searchHints">The search hints.</param>
        /// <param name="totalRecordCount">The total record count.</param>
        public SearchHintResult(IReadOnlyList<SearchHint> searchHints, int totalRecordCount)
        {
            SearchHints = searchHints;
            TotalRecordCount = totalRecordCount;
        }

        /// <summary>
        /// Gets the search hints.
        /// </summary>
        /// <value>The search hints.</value>
        public IReadOnlyList<SearchHint> SearchHints { get; }

        /// <summary>
        /// Gets the total record count.
        /// </summary>
        /// <value>The total record count.</value>
        public int TotalRecordCount { get; }
    }
}

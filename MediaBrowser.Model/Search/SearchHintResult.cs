#nullable disable
namespace MediaBrowser.Model.Search
{
    /// <summary>
    /// Class SearchHintResult.
    /// </summary>
    public class SearchHintResult
    {
        /// <summary>
        /// Gets or sets the search hints.
        /// </summary>
        /// <value>The search hints.</value>
        public SearchHint[] SearchHints { get; set; }

        /// <summary>
        /// Gets or sets the total record count.
        /// </summary>
        /// <value>The total record count.</value>
        public int TotalRecordCount { get; set; }
    }
}

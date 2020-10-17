using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Class SearchHintInfo.
    /// </summary>
    public class SearchHintInfo
    {
        /// <summary>
        /// Gets or sets the item.
        /// </summary>
        /// <value>The item.</value>
        public BaseItem Item { get; set; }

        /// <summary>
        /// Gets or sets the matched term.
        /// </summary>
        /// <value>The matched term.</value>
        public string MatchedTerm { get; set; }
    }
}

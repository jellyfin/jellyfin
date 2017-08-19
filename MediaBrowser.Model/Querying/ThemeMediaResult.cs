using MediaBrowser.Model.Dto;

namespace MediaBrowser.Model.Querying
{
    /// <summary>
    /// Class ThemeMediaResult
    /// </summary>
    public class ThemeMediaResult : QueryResult<BaseItemDto>
    {
        /// <summary>
        /// Gets or sets the owner id.
        /// </summary>
        /// <value>The owner id.</value>
        public string OwnerId { get; set; }
    }
}

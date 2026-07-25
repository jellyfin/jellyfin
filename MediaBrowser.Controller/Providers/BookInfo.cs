#nullable disable

namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// The lookup info for books.
    /// </summary>
    public class BookInfo : ItemLookupInfo
    {
        /// <summary>
        /// Gets or sets the name of the series the book belongs to.
        /// </summary>
        public string SeriesName { get; set; }
    }
}

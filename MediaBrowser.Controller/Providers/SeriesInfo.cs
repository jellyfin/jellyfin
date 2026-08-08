namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// The lookup info for series.
    /// </summary>
    public class SeriesInfo : ItemLookupInfo
    {
        /// <summary>
        /// Gets or sets the episode order the series is displayed in.
        /// </summary>
        public string? DisplayOrder { get; set; }
    }
}

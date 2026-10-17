namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// The lookup info for series.
    /// </summary>
    public class SeriesInfo : ItemLookupInfo
    {
        /// <summary>
        /// Gets or sets the canned display order group.
        /// </summary>
        public string? DisplayOrder { get; set; }
    }
}

namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// The resolution constraints.
    /// </summary>
    public class ResolutionOptions
    {
        /// <summary>
        /// Gets or sets the maximum width.
        /// </summary>
        public int? MaxWidth { get; set; }

        /// <summary>
        /// Gets or sets the maximum height.
        /// </summary>
        public int? MaxHeight { get; set; }
    }
}

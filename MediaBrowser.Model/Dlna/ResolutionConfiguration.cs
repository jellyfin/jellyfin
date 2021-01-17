namespace MediaBrowser.Model.Dlna
{
    /// <summary>
    /// Defines the <see cref="ResolutionConfiguration" />.
    /// </summary>
    public class ResolutionConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ResolutionConfiguration"/> class.
        /// </summary>
        /// <param name="maxWidth">The maxWidth<see cref="int"/>.</param>
        /// <param name="maxBitrate">The maxBitrate<see cref="int"/>.</param>
        public ResolutionConfiguration(int maxWidth, int maxBitrate)
        {
            MaxWidth = maxWidth;
            MaxBitrate = maxBitrate;
        }

        /// <summary>
        /// Gets or sets the MaxWidth.
        /// </summary>
        public int MaxWidth { get; set; }

        /// <summary>
        /// Gets or sets the MaxBitrate.
        /// </summary>
        public int MaxBitrate { get; set; }
    }
}

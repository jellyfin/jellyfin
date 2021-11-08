namespace Emby.Naming.TV
{
    /// <summary>
    /// Holder object for Series information.
    /// </summary>
    public class SeriesInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SeriesInfo"/> class.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        public SeriesInfo(string path)
        {
            Path = path;
        }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the name of the series.
        /// </summary>
        /// <value>The name of the series.</value>
        public string? Name { get; set; }
    }
}

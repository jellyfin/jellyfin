namespace Emby.Naming.TV
{
    /// <summary>
    /// Holder object for Episode information.
    /// </summary>
    public class EpisodeInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EpisodeInfo"/> class.
        /// </summary>
        /// <param name="path">Path to the file.</param>
        public EpisodeInfo(string path)
        {
            Path = path;
        }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the container.
        /// </summary>
        /// <value>The container.</value>
        public string? Container { get; set; }

        /// <summary>
        /// Gets or sets the name of the series.
        /// </summary>
        /// <value>The name of the series.</value>
        public string? SeriesName { get; set; }

        /// <summary>
        /// Gets or sets the format3 d.
        /// </summary>
        /// <value>The format3 d.</value>
        public string? Format3D { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [is3 d].
        /// </summary>
        /// <value><c>true</c> if [is3 d]; otherwise, <c>false</c>.</value>
        public bool Is3D { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is stub.
        /// </summary>
        /// <value><c>true</c> if this instance is stub; otherwise, <c>false</c>.</value>
        public bool IsStub { get; set; }

        /// <summary>
        /// Gets or sets the type of the stub.
        /// </summary>
        /// <value>The type of the stub.</value>
        public string? StubType { get; set; }

        /// <summary>
        /// Gets or sets optional season number.
        /// </summary>
        public int? SeasonNumber { get; set; }

        /// <summary>
        /// Gets or sets optional episode number.
        /// </summary>
        public int? EpisodeNumber { get; set; }

        /// <summary>
        /// Gets or sets optional ending episode number. For multi-episode files 1-13.
        /// </summary>
        public int? EndingEpisodeNumber { get; set; }

        /// <summary>
        /// Gets or sets optional year of release.
        /// </summary>
        public int? Year { get; set; }

        /// <summary>
        /// Gets or sets optional year of release.
        /// </summary>
        public int? Month { get; set; }

        /// <summary>
        /// Gets or sets optional day of release.
        /// </summary>
        public int? Day { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether by date expression was used.
        /// </summary>
        public bool IsByDate { get; set; }
    }
}

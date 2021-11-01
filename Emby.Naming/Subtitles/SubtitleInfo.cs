namespace Emby.Naming.Subtitles
{
    /// <summary>
    /// Class holding information about subtitle.
    /// </summary>
    public class SubtitleInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitleInfo"/> class.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <param name="isDefault">Is subtitle default.</param>
        /// <param name="isForced">Is subtitle forced.</param>
        public SubtitleInfo(string path, bool isDefault, bool isForced)
        {
            Path = path;
            IsDefault = isDefault;
            IsForced = isForced;
        }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the language.
        /// </summary>
        /// <value>The language.</value>
        public string? Language { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is default.
        /// </summary>
        /// <value><c>true</c> if this instance is default; otherwise, <c>false</c>.</value>
        public bool IsDefault { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is forced.
        /// </summary>
        /// <value><c>true</c> if this instance is forced; otherwise, <c>false</c>.</value>
        public bool IsForced { get; set; }
    }
}

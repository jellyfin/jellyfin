namespace Emby.Naming.ExternalFiles
{
    /// <summary>
    /// Class holding information about external files.
    /// </summary>
    public class ExternalPathParserResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExternalPathParserResult"/> class.
        /// </summary>
        /// <param name="path">Path to file.</param>
        /// <param name="isDefault">Is default.</param>
        /// <param name="isForced">Is forced.</param>
        public ExternalPathParserResult(string path, bool isDefault = false, bool isForced = false)
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
        /// Gets or sets the title.
        /// </summary>
        /// <value>The title.</value>
        public string? Title { get; set; }

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

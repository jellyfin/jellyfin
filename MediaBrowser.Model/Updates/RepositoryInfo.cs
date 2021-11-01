namespace MediaBrowser.Model.Updates
{
    /// <summary>
    /// Class RepositoryInfo.
    /// </summary>
    public class RepositoryInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the URL.
        /// </summary>
        /// <value>The URL.</value>
        public string? Url { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the repository is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled.</value>
        public bool Enabled { get; set; } = true;
    }
}

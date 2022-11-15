namespace MediaBrowser.Model.IO
{
    /// <summary>
    /// Class FileSystemEntryInfo.
    /// </summary>
    public class FileSystemEntryInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemEntryInfo" /> class.
        /// </summary>
        /// <param name="name">The filename.</param>
        /// <param name="path">The file path.</param>
        /// <param name="type">The file type.</param>
        public FileSystemEntryInfo(string name, string path, FileSystemEntryType type)
        {
            Name = name;
            Path = path;
            Type = type;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        /// <value>The type.</value>
        public FileSystemEntryType Type { get; }
    }
}

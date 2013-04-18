
namespace MediaBrowser.Model.IO
{
    /// <summary>
    /// Class FileSystemEntryInfo
    /// </summary>
    public class FileSystemEntryInfo
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>The type.</value>
        public FileSystemEntryType Type { get; set; }
    }

    /// <summary>
    /// Enum FileSystemEntryType
    /// </summary>
    public enum FileSystemEntryType
    {
        /// <summary>
        /// The file
        /// </summary>
        File,
        /// <summary>
        /// The directory
        /// </summary>
        Directory,
        /// <summary>
        /// The network computer
        /// </summary>
        NetworkComputer,
        /// <summary>
        /// The network share
        /// </summary>
        NetworkShare
    }
}

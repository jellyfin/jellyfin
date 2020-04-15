namespace Emby.Server.Implementations.IO
{
    /// <summary>
    /// Contains additional filesystem information.
    /// </summary>
    public class ExtendedFileSystemInfo
    {
        /// <summary>
        /// Gets or sets whether this is hidden.
        /// </summary>
        public bool IsHidden { get; set; }

        /// <summary>
        /// Gets or sets whether this is read-only.
        /// </summary>
        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Gets or sets whether this exists.
        /// </summary>
        public bool Exists { get; set; }
    }
}

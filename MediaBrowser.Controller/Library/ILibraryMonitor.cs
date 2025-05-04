namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// Service responsible for monitoring library filesystems for changes.
    /// </summary>
    public interface ILibraryMonitor
    {
        /// <summary>
        /// Starts this instance.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops this instance.
        /// </summary>
        void Stop();

        /// <summary>
        /// Reports the file system change beginning.
        /// </summary>
        /// <param name="path">The path.</param>
        void ReportFileSystemChangeBeginning(string path);

        /// <summary>
        /// Reports the file system change complete.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="refreshPath">if set to <c>true</c> [refresh path].</param>
        void ReportFileSystemChangeComplete(string path, bool refreshPath);

        /// <summary>
        /// Reports the file system changed.
        /// </summary>
        /// <param name="path">The path.</param>
        void ReportFileSystemChanged(string path);
    }
}

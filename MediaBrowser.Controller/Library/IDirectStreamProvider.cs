using System.IO;

namespace MediaBrowser.Controller.Library
{
    /// <summary>
    /// The direct live TV stream provider.
    /// </summary>
    /// <remarks>
    /// Deprecated.
    /// </remarks>
    public interface IDirectStreamProvider
    {
        /// <summary>
        /// Gets the live stream, optionally seeks to the end of the file first.
        /// </summary>
        /// <param name="seekNearEnd">A value indicating whether to seek to the end of the file.</param>
        /// <returns>The stream.</returns>
        Stream GetStream(bool seekNearEnd = true);
    }
}

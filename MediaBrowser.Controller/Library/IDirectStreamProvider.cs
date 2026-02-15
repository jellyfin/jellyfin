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
        /// Gets the live stream, shared streams seek to the end of the file first.
        /// </summary>
        /// <returns>The stream.</returns>
        Stream GetStream();
    }
}

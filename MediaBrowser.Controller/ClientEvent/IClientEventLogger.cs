using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.ClientEvent
{
    /// <summary>
    /// The client event logger.
    /// </summary>
    public interface IClientEventLogger
    {
        /// <summary>
        /// Writes a file to the log directory.
        /// </summary>
        /// <param name="clientName">The client name writing the document.</param>
        /// <param name="clientVersion">The client version writing the document.</param>
        /// <param name="fileContents">The file contents to write.</param>
        /// <returns>The created file name.</returns>
        Task<string> WriteDocumentAsync(
            string clientName,
            string clientVersion,
            Stream fileContents);
    }
}

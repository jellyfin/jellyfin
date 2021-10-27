using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Model.ClientLog;

namespace MediaBrowser.Controller.ClientEvent
{
    /// <summary>
    /// The client event logger.
    /// </summary>
    public interface IClientEventLogger
    {
        /// <summary>
        /// Logs the event from the client.
        /// </summary>
        /// <param name="clientLogEvent">The client log event.</param>
        void Log(ClientLogEvent clientLogEvent);

        /// <summary>
        /// Writes a file to the log directory.
        /// </summary>
        /// <param name="fileName">The file name.</param>
        /// <param name="fileContents">The file contents.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task WriteFileAsync(string fileName, Stream fileContents);
    }
}

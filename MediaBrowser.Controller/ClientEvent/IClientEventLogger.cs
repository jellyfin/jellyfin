using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Controller.Net;
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
        /// <param name="authorizationInfo">The current authorization info.</param>
        /// <param name="fileContents">The file contents to write.</param>
        /// <returns>The created file name.</returns>
        Task<string> WriteDocumentAsync(AuthorizationInfo authorizationInfo, Stream fileContents);
    }
}

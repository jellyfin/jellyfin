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
    }
}

namespace MediaBrowser.Model.Net
{
    /// <summary>
    /// Enum WebSocketState
    /// </summary>
    public enum WebSocketState
    {
        /// <summary>
        /// The none
        /// </summary>
        None,
        /// <summary>
        /// The connecting
        /// </summary>
        Connecting,
        /// <summary>
        /// The open
        /// </summary>
        Open,
        /// <summary>
        /// The close sent
        /// </summary>
        CloseSent,
        /// <summary>
        /// The close received
        /// </summary>
        CloseReceived,
        /// <summary>
        /// The closed
        /// </summary>
        Closed,
        /// <summary>
        /// The aborted
        /// </summary>
        Aborted
    }
}

#pragma warning disable CS1591
#pragma warning disable SA1600

namespace MediaBrowser.Model.Net
{
    /// <summary>
    /// Class WebSocketMessage.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class WebSocketMessage<T>
    {
        /// <summary>
        /// Gets or sets the type of the message.
        /// </summary>
        /// <value>The type of the message.</value>
        public string MessageType { get; set; }
        public string MessageId { get; set; }
        public string ServerId { get; set; }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public T Data { get; set; }
    }

}

#pragma warning disable CS1591
#pragma warning disable SA1602 // Enumeration items should be documented

using System;

namespace MediaBrowser.Model.Net
{
    /// <summary>
    /// Class WebSocketMessage.
    /// </summary>
    /// <typeparam name="T">Type.</typeparam>
    public class WebSocketMessage<T>
    {
        /// <summary>
        /// Gets or sets the type of the message.
        /// </summary>
        /// <value>The type of the message.</value>
        public string MessageType { get; set; }

        /// <summary>
        /// Gets or sets the message Id.
        /// </summary>
        public Guid MessageId { get; set; }

        /// <summary>
        /// Gets or sets the server Id.
        /// </summary>
        public string ServerId { get; set; }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public T Data { get; set; }
    }
}

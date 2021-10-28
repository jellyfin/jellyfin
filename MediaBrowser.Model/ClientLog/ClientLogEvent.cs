using System;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Model.ClientLog
{
    /// <summary>
    /// The client log event.
    /// </summary>
    public class ClientLogEvent
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ClientLogEvent"/> class.
        /// </summary>
        /// <param name="timestamp">The log timestamp.</param>
        /// <param name="level">The log level.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="clientName">The client name.</param>
        /// <param name="clientVersion">The client version.</param>
        /// <param name="deviceId">The device id.</param>
        /// <param name="message">The message.</param>
        public ClientLogEvent(
            DateTime timestamp,
            LogLevel level,
            Guid? userId,
            string clientName,
            string clientVersion,
            string deviceId,
            string message)
        {
            Timestamp = timestamp;
            UserId = userId;
            ClientName = clientName;
            ClientVersion = clientVersion;
            DeviceId = deviceId;
            Message = message;
            Level = level;
        }

        /// <summary>
        /// Gets the event timestamp.
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// Gets the log level.
        /// </summary>
        public LogLevel Level { get; }

        /// <summary>
        /// Gets the user id.
        /// </summary>
        public Guid? UserId { get; }

        /// <summary>
        /// Gets the client name.
        /// </summary>
        public string ClientName { get; }

        /// <summary>
        /// Gets the client version.
        /// </summary>
        public string ClientVersion { get; }

        ///
        /// <summary>
        /// Gets the device id.
        /// </summary>
        public string DeviceId { get; }

        /// <summary>
        /// Gets the log message.
        /// </summary>
        public string Message { get; }
    }
}

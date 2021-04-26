using System;
using MediaBrowser.Model.ClientLog;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.ClientEvent
{
    /// <inheritdoc />
    public class ClientEventLogger : IClientEventLogger
    {
        private const string LogString = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level}] [{ClientName}:{ClientVersion}]: UserId: {UserId} DeviceId: {DeviceId}{NewLine}{Message}";
        private readonly ILogger<ClientEventLogger> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientEventLogger"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{ClientEventLogger}"/> interface.</param>
        public ClientEventLogger(ILogger<ClientEventLogger> logger)
        {
            _logger = logger;
        }

        /// <inheritdoc />
        public void Log(ClientLogEvent clientLogEvent)
        {
            _logger.Log(
                LogLevel.Critical,
                LogString,
                clientLogEvent.Timestamp,
                clientLogEvent.Level.ToString(),
                clientLogEvent.ClientName,
                clientLogEvent.ClientVersion,
                clientLogEvent.UserId ?? Guid.Empty,
                clientLogEvent.DeviceId,
                Environment.NewLine,
                clientLogEvent.Message);
        }
    }
}
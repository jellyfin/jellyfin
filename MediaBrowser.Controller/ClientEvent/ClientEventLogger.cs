using System;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Model.ClientLog;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Controller.ClientEvent
{
    /// <inheritdoc />
    public class ClientEventLogger : IClientEventLogger
    {
        private const string LogString = "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Level}] [{ClientName}:{ClientVersion}]: UserId: {UserId} DeviceId: {DeviceId}{NewLine}{Message}";
        private readonly ILogger<ClientEventLogger> _logger;
        private readonly IServerApplicationPaths _applicationPaths;

        /// <summary>
        /// Initializes a new instance of the <see cref="ClientEventLogger"/> class.
        /// </summary>
        /// <param name="logger">Instance of the <see cref="ILogger{ClientEventLogger}"/> interface.</param>
        /// <param name="applicationPaths">Instance of the <see cref="IServerApplicationPaths"/> interface.</param>
        public ClientEventLogger(
            ILogger<ClientEventLogger> logger,
            IServerApplicationPaths applicationPaths)
        {
            _logger = logger;
            _applicationPaths = applicationPaths;
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

        /// <inheritdoc />
        public async Task WriteFileAsync(string fileName, Stream fileContents)
        {
            // Force naming convention: upload_YYYYMMDD_$name
            fileName = $"upload_{DateTime.UtcNow:yyyyMMdd}_{fileName}";
            var logFilePath = Path.Combine(_applicationPaths.LogDirectoryPath, fileName);
            await using var fileStream = new FileStream(logFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            await fileContents.CopyToAsync(fileStream).ConfigureAwait(false);
            await fileStream.FlushAsync().ConfigureAwait(false);
        }
    }
}

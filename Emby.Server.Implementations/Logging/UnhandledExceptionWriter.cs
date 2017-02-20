using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Logging
{
    public class UnhandledExceptionWriter
    {
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger _logger;
        private readonly ILogManager _logManager;
        private readonly IFileSystem _fileSystem;
        private readonly IConsoleLogger _console;

        public UnhandledExceptionWriter(IApplicationPaths appPaths, ILogger logger, ILogManager logManager, IFileSystem fileSystem, IConsoleLogger console)
        {
            _appPaths = appPaths;
            _logger = logger;
            _logManager = logManager;
            _fileSystem = fileSystem;
            _console = console;
        }

        public void Log(Exception ex)
        {
            _logger.ErrorException("UnhandledException", ex);
            _logManager.Flush();

            var path = Path.Combine(_appPaths.LogDirectoryPath, "unhandled_" + Guid.NewGuid() + ".txt");
            _fileSystem.CreateDirectory(Path.GetDirectoryName(path));

            var builder = LogHelper.GetLogMessage(ex);

            // Write to console just in case file logging fails
            _console.WriteLine("UnhandledException");
            _console.WriteLine(builder.ToString());

            _fileSystem.WriteAllText(path, builder.ToString());
        }
    }
}

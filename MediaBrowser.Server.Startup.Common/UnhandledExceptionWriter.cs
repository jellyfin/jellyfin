using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Implementations.Logging;
using MediaBrowser.Model.Logging;
using System;
using System.IO;

namespace MediaBrowser.Server.Startup.Common
{
    public class UnhandledExceptionWriter
    {
        private readonly IApplicationPaths _appPaths;
        private readonly ILogger _logger;
        private readonly ILogManager _logManager;

        public UnhandledExceptionWriter(IApplicationPaths appPaths, ILogger logger, ILogManager logManager)
        {
            _appPaths = appPaths;
            _logger = logger;
            _logManager = logManager;
        }

        public void Log(Exception ex)
        {
            _logger.ErrorException("UnhandledException", ex);
            _logManager.Flush();

            var path = Path.Combine(_appPaths.LogDirectoryPath, "unhandled_" + Guid.NewGuid() + ".txt");
			Directory.CreateDirectory(Path.GetDirectoryName(path));

            var builder = LogHelper.GetLogMessage(ex);

            // Write to console just in case file logging fails
            Console.WriteLine("UnhandledException");
            Console.WriteLine(builder.ToString());
            
			File.WriteAllText(path, builder.ToString());
        }
    }
}

using System;
using System.IO;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Jellyfin.Server.Migrations
{
    /// <summary>
    /// Updater that takes care of bringing configuration up to 10.5.0 standards.
    /// </summary>
    internal class DisableZealousLogging : IUpdater
    {
        /// <inheritdoc/>
        public string Name => "DisableZealousLogging";

        /// <inheritdoc/>
        // This tones down logging from some components
        public void Perform(CoreAppHost host, ILogger logger)
        {
            string configPath = Path.Combine(host.ServerConfigurationManager.ApplicationPaths.ConfigurationDirectoryPath, Program.LoggingConfigFile);
            // TODO: fix up the config
            throw new NotImplementedException("don't know how to fix logging yet");
        }
    }
}

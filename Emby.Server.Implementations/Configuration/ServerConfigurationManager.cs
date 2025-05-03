using System;
using System.Globalization;
using System.IO;
using Emby.Server.Implementations.AppBase;
using Jellyfin.Data.Events;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Configuration
{
    /// <summary>
    /// Class ServerConfigurationManager.
    /// </summary>
    public class ServerConfigurationManager : BaseConfigurationManager, IServerConfigurationManager
    {
        private readonly IConfiguration _startupConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerConfigurationManager" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        /// <param name="startupConfig">The startup configuration containing environment variables.</param>
        public ServerConfigurationManager(
            IApplicationPaths applicationPaths,
            ILoggerFactory loggerFactory,
            IXmlSerializer xmlSerializer,
            IConfiguration startupConfig)
            : base(applicationPaths, loggerFactory, xmlSerializer)
        {
            _startupConfig = startupConfig;

            // Check for environment variable override AFTER base constructor loads XML
            // You can enable metrics by setting the JELLYFIN_EnableMetrics environmental variable
            if (bool.TryParse(_startupConfig["EnableMetrics"], out var enableMetricsEnv) && enableMetricsEnv)
            {
                if (!Configuration.EnableMetrics) // Only override if not already true from XML
                {
                    Configuration.EnableMetrics = true;
                    Logger.LogInformation("Metrics enabled via JELLYFIN_EnableMetrics environment variable, overriding configuration file setting.");
                }
                else
                {
                    Logger.LogInformation("Metrics enabled via JELLYFIN_EnableMetrics environment variable (matches configuration file setting).");
                }
            }

            // Check for environment variable override for MetricsListenPort
            // You can set the metrics port by setting the JELLYFIN_MetricsListenPort environmental variable
            if (int.TryParse(_startupConfig["MetricsListenPort"], out var metricsPortEnv) && metricsPortEnv > 0)
            {
                if (Configuration.MetricsListenPort <= 0 || Configuration.MetricsListenPort != metricsPortEnv) // Only override if not set or different
                {
                    Configuration.MetricsListenPort = metricsPortEnv;
                    Logger.LogInformation("Metrics listen port set to {Port} via JELLYFIN_MetricsListenPort environment variable, overriding configuration file setting.", metricsPortEnv);
                }
                else
                {
                    Logger.LogInformation("Metrics listen port set to {Port} via JELLYFIN_MetricsListenPort environment variable (matches configuration file setting).", metricsPortEnv);
                }
            }

            UpdateMetadataPath();
        }

        /// <summary>
        /// Configuration updating event.
        /// </summary>
        public event EventHandler<GenericEventArgs<ServerConfiguration>>? ConfigurationUpdating;

        /// <summary>
        /// Gets the type of the configuration.
        /// </summary>
        /// <value>The type of the configuration.</value>
        protected override Type ConfigurationType => typeof(ServerConfiguration);

        /// <summary>
        /// Gets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        public IServerApplicationPaths ApplicationPaths => (IServerApplicationPaths)CommonApplicationPaths;

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public ServerConfiguration Configuration => (ServerConfiguration)CommonConfiguration;

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        protected override void OnConfigurationUpdated()
        {
            UpdateMetadataPath();

            base.OnConfigurationUpdated();
        }

        /// <summary>
        /// Updates the metadata path.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">If the directory does not exist, and the caller does not have the required permission to create it.</exception>
        /// <exception cref="NotSupportedException">If there is a custom path transcoding path specified, but it is invalid.</exception>
        /// <exception cref="IOException">If the directory does not exist, and it also could not be created.</exception>
        private void UpdateMetadataPath()
        {
            ((ServerApplicationPaths)ApplicationPaths).InternalMetadataPath = string.IsNullOrWhiteSpace(Configuration.MetadataPath)
                ? ApplicationPaths.DefaultInternalMetadataPath
                : Configuration.MetadataPath;
            Directory.CreateDirectory(ApplicationPaths.InternalMetadataPath);
        }

        /// <summary>
        /// Replaces the configuration.
        /// </summary>
        /// <param name="newConfiguration">The new configuration.</param>
        /// <exception cref="DirectoryNotFoundException">If the configuration path doesn't exist.</exception>
        public override void ReplaceConfiguration(BaseApplicationConfiguration newConfiguration)
        {
            var newConfig = (ServerConfiguration)newConfiguration;

            ValidateMetadataPath(newConfig);

            ConfigurationUpdating?.Invoke(this, new GenericEventArgs<ServerConfiguration>(newConfig));

            base.ReplaceConfiguration(newConfiguration);
        }

        /// <summary>
        /// Validates the metadata path.
        /// </summary>
        /// <param name="newConfig">The new configuration.</param>
        /// <exception cref="DirectoryNotFoundException">The new config path doesn't exist.</exception>
        private void ValidateMetadataPath(ServerConfiguration newConfig)
        {
            var newPath = newConfig.MetadataPath;

            if (!string.IsNullOrWhiteSpace(newPath)
                && !string.Equals(Configuration.MetadataPath, newPath, StringComparison.Ordinal))
            {
                if (!Directory.Exists(newPath))
                {
                    throw new DirectoryNotFoundException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "{0} does not exist.",
                            newPath));
                }

                EnsureWriteAccess(newPath);
            }
        }
    }
}

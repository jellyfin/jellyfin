using System;
using System.Globalization;
using System.IO;
using Emby.Server.Implementations.AppBase;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Configuration
{
    /// <summary>
    /// Class ServerConfigurationManager.
    /// </summary>
    public class ServerConfigurationManager : BaseConfigurationManager, IServerConfigurationManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ServerConfigurationManager" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="loggerFactory">The paramref name="loggerFactory" factory.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        /// <param name="fileSystem">The file system.</param>
        public ServerConfigurationManager(IApplicationPaths applicationPaths, ILoggerFactory loggerFactory, IXmlSerializer xmlSerializer, IFileSystem fileSystem)
            : base(applicationPaths, loggerFactory, xmlSerializer, fileSystem)
        {
            UpdateMetadataPath();
        }

        /// <summary>
        /// Configuration updating event.
        /// </summary>
        public event EventHandler<GenericEventArgs<ServerConfiguration>> ConfigurationUpdating;

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
        private void UpdateMetadataPath()
        {
            if (string.IsNullOrWhiteSpace(Configuration.MetadataPath))
            {
                ((ServerApplicationPaths)ApplicationPaths).InternalMetadataPath = Path.Combine(ApplicationPaths.ProgramDataPath, "metadata");
            }
            else
            {
                ((ServerApplicationPaths)ApplicationPaths).InternalMetadataPath = Configuration.MetadataPath;
            }
        }

        /// <summary>
        /// Replaces the configuration.
        /// </summary>
        /// <param name="newConfiguration">The new configuration.</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        public override void ReplaceConfiguration(BaseApplicationConfiguration newConfiguration)
        {
            var newConfig = (ServerConfiguration)newConfiguration;

            ValidateMetadataPath(newConfig);
            ValidateSslCertificate(newConfig);

            ConfigurationUpdating?.Invoke(this, new GenericEventArgs<ServerConfiguration> { Argument = newConfig });

            base.ReplaceConfiguration(newConfiguration);
        }

        /// <summary>
        /// Validates the SSL certificate.
        /// </summary>
        /// <param name="newConfig">The new configuration.</param>
        /// <exception cref="FileNotFoundException">The certificate path doesn't exist.</exception>
        private void ValidateSslCertificate(BaseApplicationConfiguration newConfig)
        {
            var serverConfig = (ServerConfiguration)newConfig;

            var newPath = serverConfig.CertificatePath;

            if (!string.IsNullOrWhiteSpace(newPath)
                && !string.Equals(Configuration.CertificatePath, newPath, StringComparison.Ordinal))
            {
                // Validate
                if (!File.Exists(newPath))
                {
                    throw new FileNotFoundException(
                        string.Format(
                            CultureInfo.InvariantCulture,
                            "Certificate file '{0}' does not exist.",
                            newPath));
                }
            }
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
                && !string.Equals(Configuration.MetadataPath, newPath,  StringComparison.Ordinal))
            {
                // Validate
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

        /// <summary>
        /// Sets all configuration values to their optimal values.
        /// </summary>
        /// <returns>If the configuration changed.</returns>
        public bool SetOptimalValues()
        {
            var config = Configuration;

            var changed = false;

            if (!config.EnableCaseSensitiveItemIds)
            {
                config.EnableCaseSensitiveItemIds = true;
                changed = true;
            }

            if (!config.SkipDeserializationForBasicTypes)
            {
                config.SkipDeserializationForBasicTypes = true;
                changed = true;
            }

            if (!config.EnableSimpleArtistDetection)
            {
                config.EnableSimpleArtistDetection = true;
                changed = true;
            }

            if (!config.EnableNormalizedItemByNameIds)
            {
                config.EnableNormalizedItemByNameIds = true;
                changed = true;
            }

            if (!config.DisableLiveTvChannelUserDataName)
            {
                config.DisableLiveTvChannelUserDataName = true;
                changed = true;
            }

            if (!config.EnableNewOmdbSupport)
            {
                config.EnableNewOmdbSupport = true;
                changed = true;
            }

            if (!config.CameraUploadUpgraded)
            {
                config.CameraUploadUpgraded = true;
                changed = true;
            }

            if (!config.CollectionsUpgraded)
            {
                config.CollectionsUpgraded = true;
                changed = true;
            }

            return changed;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using Emby.Server.Implementations.AppBase;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.IO;
using Microsoft.Extensions.Logging;
using MediaBrowser.Model.Serialization;

namespace Emby.Server.Implementations.Configuration
{
    /// <summary>
    /// Class ServerConfigurationManager
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

        public event EventHandler<GenericEventArgs<ServerConfiguration>> ConfigurationUpdating;

        /// <summary>
        /// Gets the type of the configuration.
        /// </summary>
        /// <value>The type of the configuration.</value>
        protected override Type ConfigurationType
        {
            get { return typeof(ServerConfiguration); }
        }

        /// <summary>
        /// Gets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        public IServerApplicationPaths ApplicationPaths
        {
            get { return (IServerApplicationPaths)CommonApplicationPaths; }
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public ServerConfiguration Configuration
        {
            get { return (ServerConfiguration)CommonConfiguration; }
        }

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        protected override void OnConfigurationUpdated()
        {
            UpdateMetadataPath();

            base.OnConfigurationUpdated();
        }

        public override void AddParts(IEnumerable<IConfigurationFactory> factories)
        {
            base.AddParts(factories);

            UpdateTranscodingTempPath();
        }

        /// <summary>
        /// Updates the metadata path.
        /// </summary>
        private void UpdateMetadataPath()
        {
            string metadataPath;

            if (string.IsNullOrWhiteSpace(Configuration.MetadataPath))
            {
                metadataPath = GetInternalMetadataPath();
            }
            else
            {
                metadataPath = Path.Combine(Configuration.MetadataPath, "metadata");
            }

            ((ServerApplicationPaths)ApplicationPaths).InternalMetadataPath = metadataPath;
        }

        private string GetInternalMetadataPath()
        {
            return Path.Combine(ApplicationPaths.ProgramDataPath, "metadata");
        }

        /// <summary>
        /// Updates the transcoding temporary path.
        /// </summary>
        private void UpdateTranscodingTempPath()
        {
            var encodingConfig = this.GetConfiguration<EncodingOptions>("encoding");

            ((ServerApplicationPaths)ApplicationPaths).TranscodingTempPath = string.IsNullOrEmpty(encodingConfig.TranscodingTempPath) ?
                null :
                Path.Combine(encodingConfig.TranscodingTempPath, "transcoding-temp");
        }

        protected override void OnNamedConfigurationUpdated(string key, object configuration)
        {
            base.OnNamedConfigurationUpdated(key, configuration);

            if (string.Equals(key, "encoding", StringComparison.OrdinalIgnoreCase))
            {
                UpdateTranscodingTempPath();
            }
        }

        /// <summary>
        /// Replaces the configuration.
        /// </summary>
        /// <param name="newConfiguration">The new configuration.</param>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
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
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        private void ValidateSslCertificate(BaseApplicationConfiguration newConfig)
        {
            var serverConfig = (ServerConfiguration)newConfig;

            var newPath = serverConfig.CertificatePath;

            if (!string.IsNullOrWhiteSpace(newPath)
                && !string.Equals(Configuration.CertificatePath ?? string.Empty, newPath))
            {
                // Validate
                if (!FileSystem.FileExists(newPath))
                {
                    throw new FileNotFoundException(string.Format("Certificate file '{0}' does not exist.", newPath));
                }
            }
        }

        /// <summary>
        /// Validates the metadata path.
        /// </summary>
        /// <param name="newConfig">The new configuration.</param>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        private void ValidateMetadataPath(ServerConfiguration newConfig)
        {
            var newPath = newConfig.MetadataPath;

            if (!string.IsNullOrWhiteSpace(newPath)
                && !string.Equals(Configuration.MetadataPath ?? string.Empty, newPath))
            {
                // Validate
                if (!FileSystem.DirectoryExists(newPath))
                {
                    throw new FileNotFoundException(string.Format("{0} does not exist.", newPath));
                }

                EnsureWriteAccess(newPath);
            }
        }

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

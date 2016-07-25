using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Implementations.Configuration;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.IO;
using System.Linq;
using CommonIO;

namespace MediaBrowser.Server.Implementations.Configuration
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
        /// <param name="logManager">The log manager.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        /// <param name="fileSystem">The file system.</param>
        public ServerConfigurationManager(IApplicationPaths applicationPaths, ILogManager logManager, IXmlSerializer xmlSerializer, IFileSystem fileSystem)
            : base(applicationPaths, logManager, xmlSerializer, fileSystem)
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

            ((ServerApplicationPaths)ApplicationPaths).ItemsByNamePath = ((ServerApplicationPaths)ApplicationPaths).InternalMetadataPath;
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

            ValidatePathSubstitutions(newConfig);
            ValidateMetadataPath(newConfig);
            ValidateSslCertificate(newConfig);

            EventHelper.FireEventIfNotNull(ConfigurationUpdating, this, new GenericEventArgs<ServerConfiguration> { Argument = newConfig }, Logger);

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

        private void ValidatePathSubstitutions(ServerConfiguration newConfig)
        {
            foreach (var map in newConfig.PathSubstitutions)
            {
                if (string.IsNullOrWhiteSpace(map.From) || string.IsNullOrWhiteSpace(map.To))
                {
                    throw new ArgumentException("Invalid path substitution");
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
                    throw new DirectoryNotFoundException(string.Format("{0} does not exist.", newPath));
                }

                EnsureWriteAccess(newPath);
            }
        }

        public void DisableMetadataService(string service)
        {
            DisableMetadataService(typeof(Movie), Configuration, service);
            DisableMetadataService(typeof(Episode), Configuration, service);
            DisableMetadataService(typeof(Series), Configuration, service);
            DisableMetadataService(typeof(Season), Configuration, service);
            DisableMetadataService(typeof(MusicArtist), Configuration, service);
            DisableMetadataService(typeof(MusicAlbum), Configuration, service);
            DisableMetadataService(typeof(MusicVideo), Configuration, service);
            DisableMetadataService(typeof(Video), Configuration, service);
        }

        private void DisableMetadataService(Type type, ServerConfiguration config, string service)
        {
            var options = GetMetadataOptions(type, config);

            if (!options.DisabledMetadataSavers.Contains(service, StringComparer.OrdinalIgnoreCase))
            {
                var list = options.DisabledMetadataSavers.ToList();

                list.Add(service);

                options.DisabledMetadataSavers = list.ToArray();
            }
        }

        private MetadataOptions GetMetadataOptions(Type type, ServerConfiguration config)
        {
            var options = config.MetadataOptions
                .FirstOrDefault(i => string.Equals(i.ItemType, type.Name, StringComparison.OrdinalIgnoreCase));

            if (options == null)
            {
                var list = config.MetadataOptions.ToList();

                options = new MetadataOptions
                {
                    ItemType = type.Name
                };

                list.Add(options);

                config.MetadataOptions = list.ToArray();
            }

            return options;
        }
    }
}

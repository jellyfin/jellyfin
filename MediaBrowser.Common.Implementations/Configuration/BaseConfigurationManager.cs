using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using CommonIO;
using MediaBrowser.Common.Extensions;

namespace MediaBrowser.Common.Implementations.Configuration
{
    /// <summary>
    /// Class BaseConfigurationManager
    /// </summary>
    public abstract class BaseConfigurationManager : IConfigurationManager
    {
        /// <summary>
        /// Gets the type of the configuration.
        /// </summary>
        /// <value>The type of the configuration.</value>
        protected abstract Type ConfigurationType { get; }

        /// <summary>
        /// Occurs when [configuration updated].
        /// </summary>
        public event EventHandler<EventArgs> ConfigurationUpdated;

        /// <summary>
        /// Occurs when [configuration updating].
        /// </summary>
        public event EventHandler<ConfigurationUpdateEventArgs> NamedConfigurationUpdating;

        /// <summary>
        /// Occurs when [named configuration updated].
        /// </summary>
        public event EventHandler<ConfigurationUpdateEventArgs> NamedConfigurationUpdated;

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; private set; }
        /// <summary>
        /// Gets the XML serializer.
        /// </summary>
        /// <value>The XML serializer.</value>
        protected IXmlSerializer XmlSerializer { get; private set; }

        /// <summary>
        /// Gets or sets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        public IApplicationPaths CommonApplicationPaths { get; private set; }
        public readonly IFileSystem FileSystem;

        /// <summary>
        /// The _configuration loaded
        /// </summary>
        private bool _configurationLoaded;
        /// <summary>
        /// The _configuration sync lock
        /// </summary>
        private object _configurationSyncLock = new object();
        /// <summary>
        /// The _configuration
        /// </summary>
        private BaseApplicationConfiguration _configuration;
        /// <summary>
        /// Gets the system configuration
        /// </summary>
        /// <value>The configuration.</value>
        public BaseApplicationConfiguration CommonConfiguration
        {
            get
            {
                // Lazy load
                LazyInitializer.EnsureInitialized(ref _configuration, ref _configurationLoaded, ref _configurationSyncLock, () => (BaseApplicationConfiguration)ConfigurationHelper.GetXmlConfiguration(ConfigurationType, CommonApplicationPaths.SystemConfigurationFilePath, XmlSerializer));
                return _configuration;
            }
            protected set
            {
                _configuration = value;

                _configurationLoaded = value != null;
            }
        }

        private ConfigurationStore[] _configurationStores = { };
        private IConfigurationFactory[] _configurationFactories = { };

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseConfigurationManager" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="logManager">The log manager.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        protected BaseConfigurationManager(IApplicationPaths applicationPaths, ILogManager logManager, IXmlSerializer xmlSerializer, IFileSystem fileSystem)
        {
            CommonApplicationPaths = applicationPaths;
            XmlSerializer = xmlSerializer;
            FileSystem = fileSystem;
            Logger = logManager.GetLogger(GetType().Name);

            UpdateCachePath();
        }

        public virtual void AddParts(IEnumerable<IConfigurationFactory> factories)
        {
            _configurationFactories = factories.ToArray();

            _configurationStores = _configurationFactories
                .SelectMany(i => i.GetConfigurations())
                .ToArray();
        }

        /// <summary>
        /// Saves the configuration.
        /// </summary>
        public void SaveConfiguration()
        {
            Logger.Info("Saving system configuration");
            var path = CommonApplicationPaths.SystemConfigurationFilePath;

            Directory.CreateDirectory(Path.GetDirectoryName(path));

            lock (_configurationSyncLock)
            {
                XmlSerializer.SerializeToFile(CommonConfiguration, path);
            }

            OnConfigurationUpdated();
        }

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        protected virtual void OnConfigurationUpdated()
        {
            UpdateCachePath();

            EventHelper.QueueEventIfNotNull(ConfigurationUpdated, this, EventArgs.Empty, Logger);
        }

        /// <summary>
        /// Replaces the configuration.
        /// </summary>
        /// <param name="newConfiguration">The new configuration.</param>
        /// <exception cref="System.ArgumentNullException">newConfiguration</exception>
        public virtual void ReplaceConfiguration(BaseApplicationConfiguration newConfiguration)
        {
            if (newConfiguration == null)
            {
                throw new ArgumentNullException("newConfiguration");
            }

            ValidateCachePath(newConfiguration);

            CommonConfiguration = newConfiguration;
            SaveConfiguration();
        }

        /// <summary>
        /// Updates the items by name path.
        /// </summary>
        private void UpdateCachePath()
        {
            string cachePath;

            if (string.IsNullOrWhiteSpace(CommonConfiguration.CachePath))
            {
                cachePath = null;
            }
            else
            {
                cachePath = Path.Combine(CommonConfiguration.CachePath, "cache");
            }

            ((BaseApplicationPaths)CommonApplicationPaths).CachePath = cachePath;
        }

        /// <summary>
        /// Replaces the cache path.
        /// </summary>
        /// <param name="newConfig">The new configuration.</param>
        /// <exception cref="System.IO.DirectoryNotFoundException"></exception>
        private void ValidateCachePath(BaseApplicationConfiguration newConfig)
        {
            var newPath = newConfig.CachePath;

            if (!string.IsNullOrWhiteSpace(newPath)
                && !string.Equals(CommonConfiguration.CachePath ?? string.Empty, newPath))
            {
                // Validate
                if (!Directory.Exists(newPath))
                {
                    throw new DirectoryNotFoundException(string.Format("{0} does not exist.", newPath));
                }

                EnsureWriteAccess(newPath);
            }
        }

        protected void EnsureWriteAccess(string path)
        {
            var file = Path.Combine(path, Guid.NewGuid().ToString());

            FileSystem.WriteAllText(file, string.Empty);
            FileSystem.DeleteFile(file);
        }

        private readonly ConcurrentDictionary<string, object> _configurations = new ConcurrentDictionary<string, object>();

        private string GetConfigurationFile(string key)
        {
            return Path.Combine(CommonApplicationPaths.ConfigurationDirectoryPath, key.ToLower() + ".xml");
        }

        public object GetConfiguration(string key)
        {
            return _configurations.GetOrAdd(key, k =>
            {
                var file = GetConfigurationFile(key);

                var configurationInfo = _configurationStores
                    .FirstOrDefault(i => string.Equals(i.Key, key, StringComparison.OrdinalIgnoreCase));

                if (configurationInfo == null)
                {
                    throw new ResourceNotFoundException("Configuration with key " + key + " not found.");
                }

                var configurationType = configurationInfo.ConfigurationType;

                lock (_configurationSyncLock)
                {
                    return LoadConfiguration(file, configurationType);
                }
            });
        }

        private object LoadConfiguration(string path, Type configurationType)
        {
            try
            {
                return XmlSerializer.DeserializeFromFile(configurationType, path);
            }
            catch (FileNotFoundException)
            {
                return Activator.CreateInstance(configurationType);
            }
            catch (DirectoryNotFoundException)
            {
                return Activator.CreateInstance(configurationType);
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error loading configuration file: {0}", ex, path);

                return Activator.CreateInstance(configurationType);
            }
        }

        public void SaveConfiguration(string key, object configuration)
        {
            var configurationStore = GetConfigurationStore(key);
            var configurationType = configurationStore.ConfigurationType;

            if (configuration.GetType() != configurationType)
            {
                throw new ArgumentException("Expected configuration type is " + configurationType.Name);
            }

            var validatingStore = configurationStore as IValidatingConfiguration;
            if (validatingStore != null)
            {
                var currentConfiguration = GetConfiguration(key);

                validatingStore.Validate(currentConfiguration, configuration);
            }

            EventHelper.FireEventIfNotNull(NamedConfigurationUpdating, this, new ConfigurationUpdateEventArgs
            {
                Key = key,
                NewConfiguration = configuration

            }, Logger);

            _configurations.AddOrUpdate(key, configuration, (k, v) => configuration);

            var path = GetConfigurationFile(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            lock (_configurationSyncLock)
            {
                XmlSerializer.SerializeToFile(configuration, path);
            }

            OnNamedConfigurationUpdated(key, configuration);
        }

        protected virtual void OnNamedConfigurationUpdated(string key, object configuration)
        {
            EventHelper.FireEventIfNotNull(NamedConfigurationUpdated, this, new ConfigurationUpdateEventArgs
            {
                Key = key,
                NewConfiguration = configuration

            }, Logger);
        }

        public Type GetConfigurationType(string key)
        {
            return GetConfigurationStore(key)
                .ConfigurationType;
        }

        private ConfigurationStore GetConfigurationStore(string key)
        {
            return _configurationStores
                .First(i => string.Equals(i.Key, key, StringComparison.OrdinalIgnoreCase));
        }
    }
}

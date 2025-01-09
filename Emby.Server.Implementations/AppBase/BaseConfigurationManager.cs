using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.AppBase
{
    /// <summary>
    /// Class BaseConfigurationManager.
    /// </summary>
    public abstract class BaseConfigurationManager : IConfigurationManager
    {
        private readonly ConcurrentDictionary<string, object> _configurations = new();
        private readonly Lock _configurationSyncLock = new();

        private ConfigurationStore[] _configurationStores = Array.Empty<ConfigurationStore>();
        private IConfigurationFactory[] _configurationFactories = Array.Empty<IConfigurationFactory>();

        /// <summary>
        /// The _configuration.
        /// </summary>
        private BaseApplicationConfiguration? _configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseConfigurationManager" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        protected BaseConfigurationManager(
            IApplicationPaths applicationPaths,
            ILoggerFactory loggerFactory,
            IXmlSerializer xmlSerializer)
        {
            CommonApplicationPaths = applicationPaths;
            XmlSerializer = xmlSerializer;
            Logger = loggerFactory.CreateLogger<BaseConfigurationManager>();

            UpdateCachePath();
        }

        /// <summary>
        /// Occurs when [configuration updated].
        /// </summary>
        public event EventHandler<EventArgs>? ConfigurationUpdated;

        /// <summary>
        /// Occurs when [configuration updating].
        /// </summary>
        public event EventHandler<ConfigurationUpdateEventArgs>? NamedConfigurationUpdating;

        /// <summary>
        /// Occurs when [named configuration updated].
        /// </summary>
        public event EventHandler<ConfigurationUpdateEventArgs>? NamedConfigurationUpdated;

        /// <summary>
        /// Gets the type of the configuration.
        /// </summary>
        /// <value>The type of the configuration.</value>
        protected abstract Type ConfigurationType { get; }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger<BaseConfigurationManager> Logger { get; private set; }

        /// <summary>
        /// Gets the XML serializer.
        /// </summary>
        /// <value>The XML serializer.</value>
        protected IXmlSerializer XmlSerializer { get; private set; }

        /// <summary>
        /// Gets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        public IApplicationPaths CommonApplicationPaths { get; private set; }

        /// <summary>
        /// Gets or sets the system configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public BaseApplicationConfiguration CommonConfiguration
        {
            get
            {
                if (_configuration is not null)
                {
                    return _configuration;
                }

                lock (_configurationSyncLock)
                {
                    if (_configuration is not null)
                    {
                        return _configuration;
                    }

                    return _configuration = (BaseApplicationConfiguration)ConfigurationHelper.GetXmlConfiguration(ConfigurationType, CommonApplicationPaths.SystemConfigurationFilePath, XmlSerializer);
                }
            }

            protected set
            {
                _configuration = value;
            }
        }

        /// <summary>
        /// Manually pre-loads a factory so that it is available pre system initialisation.
        /// </summary>
        /// <typeparam name="T">Class to register.</typeparam>
        public virtual void RegisterConfiguration<T>()
            where T : IConfigurationFactory
        {
            IConfigurationFactory factory = Activator.CreateInstance<T>();

            if (_configurationFactories is null)
            {
                _configurationFactories = [factory];
            }
            else
            {
                _configurationFactories = [.._configurationFactories, factory];
            }

            _configurationStores = _configurationFactories
                .SelectMany(i => i.GetConfigurations())
                .ToArray();
        }

        /// <summary>
        /// Adds parts.
        /// </summary>
        /// <param name="factories">The configuration factories.</param>
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
            Logger.LogInformation("Saving system configuration");
            var path = CommonApplicationPaths.SystemConfigurationFilePath;

            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Path can't be a root directory."));

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
        /// <exception cref="ArgumentNullException"><c>newConfiguration</c> is <c>null</c>.</exception>
        public virtual void ReplaceConfiguration(BaseApplicationConfiguration newConfiguration)
        {
            ArgumentNullException.ThrowIfNull(newConfiguration);

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

            // If the configuration file has no entry (i.e. not set in UI)
            if (string.IsNullOrWhiteSpace(CommonConfiguration.CachePath))
            {
                // If the current live configuration has no entry (i.e. not set on CLI/envvars, during startup)
                if (string.IsNullOrWhiteSpace(((BaseApplicationPaths)CommonApplicationPaths).CachePath))
                {
                    // Set cachePath to a default value under ProgramDataPath
                    cachePath = Path.Combine(((BaseApplicationPaths)CommonApplicationPaths).ProgramDataPath, "cache");
                }
                else
                {
                    // Set cachePath to the existing live value; will require restart if UI value is removed (but not replaced)
                    // TODO: Figure out how to re-grab this from the CLI/envvars while running
                    cachePath = ((BaseApplicationPaths)CommonApplicationPaths).CachePath;
                }
            }
            else
            {
                // Set cachePath to the new UI-set value
                cachePath = CommonConfiguration.CachePath;
            }

            Logger.LogInformation("Setting cache path: {Path}", cachePath);
            ((BaseApplicationPaths)CommonApplicationPaths).CachePath = cachePath;
        }

        /// <summary>
        /// Replaces the cache path.
        /// </summary>
        /// <param name="newConfig">The new configuration.</param>
        /// <exception cref="DirectoryNotFoundException">The new cache path doesn't exist.</exception>
        private void ValidateCachePath(BaseApplicationConfiguration newConfig)
        {
            var newPath = newConfig.CachePath;

            if (!string.IsNullOrWhiteSpace(newPath)
                && !string.Equals(CommonConfiguration.CachePath ?? string.Empty, newPath, StringComparison.Ordinal))
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
        /// Ensures that we have write access to the path.
        /// </summary>
        /// <param name="path">The path.</param>
        protected void EnsureWriteAccess(string path)
        {
            var file = Path.Combine(path, Guid.NewGuid().ToString());
            File.WriteAllText(file, string.Empty);
            File.Delete(file);
        }

        private string GetConfigurationFile(string key)
        {
            return Path.Combine(CommonApplicationPaths.ConfigurationDirectoryPath, key.ToLowerInvariant() + ".xml");
        }

        /// <inheritdoc />
        public object GetConfiguration(string key)
        {
            return _configurations.GetOrAdd(
                key,
                static (k, configurationManager) =>
                {
                    var file = configurationManager.GetConfigurationFile(k);

                    var configurationInfo = Array.Find(
                        configurationManager._configurationStores,
                        i => string.Equals(i.Key, k, StringComparison.OrdinalIgnoreCase));

                    if (configurationInfo is null)
                    {
                        throw new ResourceNotFoundException("Configuration with key " + k + " not found.");
                    }

                    var configurationType = configurationInfo.ConfigurationType;

                    lock (configurationManager._configurationSyncLock)
                    {
                        return configurationManager.LoadConfiguration(file, configurationType);
                    }
                },
                this);
        }

        private object LoadConfiguration(string path, Type configurationType)
        {
            try
            {
                if (File.Exists(path))
                {
                    return XmlSerializer.DeserializeFromFile(configurationType, path);
                }
            }
            catch (Exception ex) when (ex is not IOException)
            {
                Logger.LogError(ex, "Error loading configuration file: {Path}", path);
            }

            return Activator.CreateInstance(configurationType)
                ?? throw new InvalidOperationException("Configuration type can't be Nullable<T>.");
        }

        /// <inheritdoc />
        public void SaveConfiguration(string key, object configuration)
        {
            var configurationStore = GetConfigurationStore(key);
            var configurationType = configurationStore.ConfigurationType;

            if (configuration.GetType() != configurationType)
            {
                throw new ArgumentException("Expected configuration type is " + configurationType.Name);
            }

            if (configurationStore is IValidatingConfiguration validatingStore)
            {
                var currentConfiguration = GetConfiguration(key);

                validatingStore.Validate(currentConfiguration, configuration);
            }

            NamedConfigurationUpdating?.Invoke(this, new ConfigurationUpdateEventArgs(key, configuration));

            _configurations.AddOrUpdate(key, configuration, (_, _) => configuration);

            var path = GetConfigurationFile(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Path can't be a root directory."));

            lock (_configurationSyncLock)
            {
                XmlSerializer.SerializeToFile(configuration, path);
            }

            OnNamedConfigurationUpdated(key, configuration);
        }

        /// <summary>
        /// Event handler for when a named configuration has been updated.
        /// </summary>
        /// <param name="key">The key of the configuration.</param>
        /// <param name="configuration">The old configuration.</param>
        protected virtual void OnNamedConfigurationUpdated(string key, object configuration)
        {
            NamedConfigurationUpdated?.Invoke(this, new ConfigurationUpdateEventArgs(key, configuration));
        }

        /// <inheritdoc />
        public ConfigurationStore[] GetConfigurationStores()
        {
            return _configurationStores;
        }

        /// <inheritdoc />
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using Emby.Server.Implementations.AppBase;
using Jellyfin.Data.Events;
using Jellyfin.Database.Implementations.DbConfiguration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Branding;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.LiveTv;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Configuration
{
    /// <summary>
    /// Class ServerConfigurationManager.
    /// </summary>
    [Obsolete("See IConfigurationManager")]
    public class ServerConfigurationManager : MediaBrowser.Common.Configuration.IConfigurationManager, IServerConfigurationManager
    {
        private static readonly JsonSerializerOptions _writeOptions = new()
        {
            WriteIndented = true
        };

        private static readonly Dictionary<string, (Type Type, string SectionKey, string FileName)> _knownConfigurations =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["system"] = (typeof(ServerConfiguration), "ServerConfiguration", "system.json"),
                ["encoding"] = (typeof(EncodingOptions), "EncodingOptions", "encoding.json"),
                ["network"] = (typeof(NetworkConfiguration), "NetworkConfiguration", "network.json"),
                ["branding"] = (typeof(BrandingOptions), "BrandingOptions", "branding.json"),
                ["database"] = (typeof(DatabaseConfigurationOptions), "DatabaseConfiguration", "database.json"),
                ["livetv"] = (typeof(LiveTvOptions), "LiveTvOptions", "livetv.json"),
                ["xbmcmetadata"] = (typeof(XbmcMetadataOptions), "XbmcMetadataOptions", "xbmcmetadata.json")
            };

        private readonly ConcurrentDictionary<string, object> _configurations = new(StringComparer.OrdinalIgnoreCase);
        private readonly Lock _configurationSyncLock = new();
        private readonly ILogger<ServerConfigurationManager> _logger;
        private readonly IApplicationPaths _applicationPaths;
        private readonly IXmlSerializer _xmlSerializer;
        private readonly IConfiguration _configuration;
        private readonly IConfigurationRoot? _configurationRoot;
        private BaseApplicationConfiguration? _commonConfiguration;
        private IConfigurationFactory[] _configurationFactories = Array.Empty<IConfigurationFactory>();
        private ConfigurationStore[] _configurationStores = GetKnownConfigurationStores();

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerConfigurationManager" /> class.
        /// </summary>
        /// <param name="applicationPaths">The application paths.</param>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <param name="xmlSerializer">The XML serializer.</param>
        /// <param name="configuration">The startup configuration root.</param>
        public ServerConfigurationManager(
            IApplicationPaths applicationPaths,
            ILoggerFactory loggerFactory,
            IXmlSerializer xmlSerializer,
            IConfiguration configuration)
        {
            _applicationPaths = applicationPaths;
            _logger = loggerFactory.CreateLogger<ServerConfigurationManager>();
            _xmlSerializer = xmlSerializer;
            _configuration = configuration;
            _configurationRoot = configuration as IConfigurationRoot;

            _commonConfiguration = LoadBoundConfiguration<ServerConfiguration>("system");
            UpdateCachePath();
            UpdateMetadataPath();
        }

        /// <inheritdoc />
        public event EventHandler<ConfigurationUpdateEventArgs>? NamedConfigurationUpdating;

        /// <inheritdoc />
        public event EventHandler<EventArgs>? ConfigurationUpdated;

        /// <inheritdoc />
        public event EventHandler<ConfigurationUpdateEventArgs>? NamedConfigurationUpdated;

        /// <inheritdoc />
        public IApplicationPaths CommonApplicationPaths => _applicationPaths;

        /// <inheritdoc />
        public BaseApplicationConfiguration CommonConfiguration => _commonConfiguration ??= LoadBoundConfiguration<ServerConfiguration>("system");

        /// <summary>
        /// Gets the application paths.
        /// </summary>
        /// <value>The application paths.</value>
        public IServerApplicationPaths ApplicationPaths => (IServerApplicationPaths)_applicationPaths;

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        /// <value>The configuration.</value>
        public ServerConfiguration Configuration => (ServerConfiguration)CommonConfiguration;

        /// <inheritdoc />
        public void SaveConfiguration()
        {
            SaveConfiguration("system", CommonConfiguration);
            EventHelper.QueueEventIfNotNull(ConfigurationUpdated, this, EventArgs.Empty, _logger);
        }

        /// <inheritdoc />
        public void ReplaceConfiguration(BaseApplicationConfiguration newConfiguration)
        {
            ArgumentNullException.ThrowIfNull(newConfiguration);
            ValidateCachePath(newConfiguration);
            _commonConfiguration = newConfiguration;
            SaveConfiguration();
        }

        /// <inheritdoc />
        public void RegisterConfiguration<T>()
            where T : IConfigurationFactory
        {
            IConfigurationFactory factory = Activator.CreateInstance<T>();
            _configurationFactories = [.. _configurationFactories, factory];
            _configurationStores = [.. GetKnownConfigurationStores(), .. _configurationFactories.SelectMany(i => i.GetConfigurations())];
        }

        /// <inheritdoc />
        public object GetConfiguration(string key)
        {
            if (string.Equals(key, "system", StringComparison.OrdinalIgnoreCase))
            {
                return CommonConfiguration;
            }

            if (_knownConfigurations.TryGetValue(key, out _))
            {
                return _configurations.GetOrAdd(key, static (configKey, manager) => manager.LoadKnownConfiguration(configKey), this);
            }

            return _configurations.GetOrAdd(key, static (configKey, manager) => manager.LoadPluginConfiguration(configKey), this);
        }

        /// <inheritdoc />
        public ConfigurationStore[] GetConfigurationStores()
        {
            return _configurationStores;
        }

        /// <inheritdoc />
        public Type GetConfigurationType(string key)
        {
            if (_knownConfigurations.TryGetValue(key, out var knownConfiguration))
            {
                return knownConfiguration.Type;
            }

            return GetConfigurationStore(key).ConfigurationType;
        }

        /// <inheritdoc />
        public void SaveConfiguration(string key, object configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            var configurationType = GetConfigurationType(key);
            if (configuration.GetType() != configurationType)
            {
                throw new ArgumentException("Expected configuration type is " + configurationType.Name, nameof(configuration));
            }

            NamedConfigurationUpdating?.Invoke(this, new ConfigurationUpdateEventArgs(key, configuration));

            _configurations[key] = configuration;

            if (string.Equals(key, "system", StringComparison.OrdinalIgnoreCase))
            {
                _commonConfiguration = (BaseApplicationConfiguration)configuration;
                WriteKnownConfiguration(key, configuration);
                UpdateCachePath();
                UpdateMetadataPath();
                EventHelper.QueueEventIfNotNull(ConfigurationUpdated, this, EventArgs.Empty, _logger);
            }
            else if (_knownConfigurations.ContainsKey(key))
            {
                WriteKnownConfiguration(key, configuration);
            }
            else
            {
                SavePluginConfiguration(key, configuration);
            }

            NamedConfigurationUpdated?.Invoke(this, new ConfigurationUpdateEventArgs(key, configuration));
        }

        /// <inheritdoc />
        public void AddParts(IEnumerable<IConfigurationFactory> factories)
        {
            _configurationFactories = factories.ToArray();
            _configurationStores = [.. GetKnownConfigurationStores(), .. _configurationFactories.SelectMany(i => i.GetConfigurations())];
        }

        /// <summary>
        /// Called when [configuration updated].
        /// </summary>
        protected virtual void OnConfigurationUpdated()
        {
            UpdateMetadataPath();
        }

        private static ConfigurationStore[] GetKnownConfigurationStores()
        {
            return _knownConfigurations
                .Where(static pair => !string.Equals(pair.Key, "system", StringComparison.OrdinalIgnoreCase))
                .Select(static pair => new ConfigurationStore
                {
                    Key = pair.Key,
                    ConfigurationType = pair.Value.Type
                })
                .ToArray();
        }

        private T LoadBoundConfiguration<T>(string key)
            where T : class, new()
        {
            var configurationEntry = _knownConfigurations[key];
            var value = _configuration.GetSection(configurationEntry.SectionKey).Get(configurationEntry.Type) as T;
            return value ?? new T();
        }

        private object LoadKnownConfiguration(string key)
        {
            var configurationEntry = _knownConfigurations[key];
            return _configuration.GetSection(configurationEntry.SectionKey).Get(configurationEntry.Type)
                ?? Activator.CreateInstance(configurationEntry.Type)
                ?? throw new InvalidOperationException("Configuration type can't be Nullable<T>.");
        }

        private object LoadPluginConfiguration(string key)
        {
            var configurationStore = GetConfigurationStore(key);
            var path = GetPluginConfigurationFile(key);

            try
            {
                if (File.Exists(path))
                {
                    lock (_configurationSyncLock)
                    {
                        return _xmlSerializer.DeserializeFromFile(configurationStore.ConfigurationType, path);
                    }
                }
            }
            catch (Exception ex) when (ex is not IOException)
            {
                _logger.LogError(ex, "Error loading configuration file: {Path}", path);
            }

            return Activator.CreateInstance(configurationStore.ConfigurationType)
                ?? throw new InvalidOperationException("Configuration type can't be Nullable<T>.");
        }

        private void WriteKnownConfiguration(string key, object value)
        {
            var configurationEntry = _knownConfigurations[key];
            var filePath = Path.Combine(_applicationPaths.ConfigurationDirectoryPath, configurationEntry.FileName);
            JsonObject root;

            if (File.Exists(filePath))
            {
                try
                {
                    root = JsonNode.Parse(File.ReadAllText(filePath)) as JsonObject ?? new JsonObject();
                }
                catch (JsonException)
                {
                    root = new JsonObject();
                }
            }
            else
            {
                root = new JsonObject();
            }

            root[configurationEntry.SectionKey] = JsonSerializer.SerializeToNode(value, configurationEntry.Type);

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(filePath, root.ToJsonString(_writeOptions));
            _configurationRoot?.Reload();

            if (string.Equals(key, "system", StringComparison.OrdinalIgnoreCase))
            {
                OnConfigurationUpdated();
            }
        }

        private void SavePluginConfiguration(string key, object configuration)
        {
            var path = GetPluginConfigurationFile(key);
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? throw new InvalidOperationException("Path can't be a root directory."));

            lock (_configurationSyncLock)
            {
                _xmlSerializer.SerializeToFile(configuration, path);
            }
        }

        private string GetPluginConfigurationFile(string key)
        {
            return Path.Combine(_applicationPaths.ConfigurationDirectoryPath, key.ToLowerInvariant() + ".xml");
        }

        private ConfigurationStore GetConfigurationStore(string key)
        {
            return _configurationStores.First(i => string.Equals(i.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        private void UpdateCachePath()
        {
            string cachePath;

            if (string.IsNullOrWhiteSpace(CommonConfiguration.CachePath))
            {
                if (string.IsNullOrWhiteSpace(((BaseApplicationPaths)_applicationPaths).CachePath))
                {
                    cachePath = Path.Combine(((BaseApplicationPaths)_applicationPaths).ProgramDataPath, "cache");
                }
                else
                {
                    cachePath = ((BaseApplicationPaths)_applicationPaths).CachePath;
                }
            }
            else
            {
                cachePath = CommonConfiguration.CachePath;
            }

            _logger.LogInformation("Setting cache path: {Path}", cachePath);
            ((BaseApplicationPaths)_applicationPaths).CachePath = cachePath;
            _applicationPaths.CreateAndCheckMarker(((BaseApplicationPaths)_applicationPaths).CachePath, "cache");
        }

        private void ValidateCachePath(BaseApplicationConfiguration newConfiguration)
        {
            var newPath = newConfiguration.CachePath;

            if (!string.IsNullOrWhiteSpace(newPath)
                && !string.Equals(CommonConfiguration.CachePath ?? string.Empty, newPath, StringComparison.Ordinal))
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

        private static void EnsureWriteAccess(string path)
        {
            var file = Path.Combine(path, Guid.NewGuid().ToString());
            File.WriteAllText(file, string.Empty);
            File.Delete(file);
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
    }
}

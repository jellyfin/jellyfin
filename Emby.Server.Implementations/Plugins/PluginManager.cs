using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library;
using Jellyfin.Extensions.Json;
using Jellyfin.Extensions.Json.Converters;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Updates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Plugins
{
    /// <summary>
    /// Defines the <see cref="PluginManager" />.
    /// </summary>
    public sealed class PluginManager : IPluginManager, IDisposable
    {
        private const string MetafileName = "meta.json";

        private readonly string _pluginsPath;
        private readonly Version _appVersion;
        private readonly List<AssemblyLoadContext> _assemblyLoadContexts;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ILogger<PluginManager> _logger;
        private readonly IServerApplicationHost _appHost;
        private readonly ServerConfiguration _config;
        private readonly List<LocalPlugin> _plugins;
        private readonly Version _minimumVersion;

        private IHttpClientFactory? _httpClientFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginManager"/> class.
        /// </summary>
        /// <param name="logger">The <see cref="ILogger{PluginManager}"/>.</param>
        /// <param name="appHost">The <see cref="IServerApplicationHost"/>.</param>
        /// <param name="config">The <see cref="ServerConfiguration"/>.</param>
        /// <param name="pluginsPath">The plugin path.</param>
        /// <param name="appVersion">The application version.</param>
        public PluginManager(
            ILogger<PluginManager> logger,
            IServerApplicationHost appHost,
            ServerConfiguration config,
            string pluginsPath,
            Version appVersion)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _pluginsPath = pluginsPath;
            _appVersion = appVersion ?? throw new ArgumentNullException(nameof(appVersion));
            _jsonOptions = new JsonSerializerOptions(JsonDefaults.Options)
            {
                WriteIndented = true
            };

            // We need to use the default GUID converter, so we need to remove any custom ones.
            for (int a = _jsonOptions.Converters.Count - 1; a >= 0; a--)
            {
                if (_jsonOptions.Converters[a] is JsonGuidConverter convertor)
                {
                    _jsonOptions.Converters.Remove(convertor);
                    break;
                }
            }

            _config = config;
            _appHost = appHost;
            _minimumVersion = new Version(0, 0, 0, 1);
            _plugins = Directory.Exists(_pluginsPath) ? DiscoverPlugins().ToList() : new List<LocalPlugin>();

            _assemblyLoadContexts = new List<AssemblyLoadContext>();
        }

        private IHttpClientFactory HttpClientFactory
        {
            get
            {
                return _httpClientFactory ??= _appHost.Resolve<IHttpClientFactory>();
            }
        }

        /// <summary>
        /// Gets the Plugins.
        /// </summary>
        public IReadOnlyList<LocalPlugin> Plugins => _plugins;

        /// <summary>
        /// Returns all the assemblies.
        /// </summary>
        /// <returns>An IEnumerable{Assembly}.</returns>
        public IEnumerable<Assembly> LoadAssemblies()
        {
            // Attempt to remove any deleted plugins and change any successors to be active.
            for (int i = _plugins.Count - 1; i >= 0; i--)
            {
                var plugin = _plugins[i];
                if (plugin.Manifest.Status == PluginStatus.Deleted && DeletePlugin(plugin))
                {
                    // See if there is another version, and if so make that active.
                    ProcessAlternative(plugin);
                }
            }

            // Now load the assemblies..
            foreach (var plugin in _plugins)
            {
                UpdatePluginSupersededStatus(plugin);

                if (plugin.IsEnabledAndSupported == false)
                {
                    _logger.LogInformation("Skipping disabled plugin {Version} of {Name} ", plugin.Version, plugin.Name);
                    continue;
                }

                var assemblyLoadContext = new PluginLoadContext(plugin.Path);
                _assemblyLoadContexts.Add(assemblyLoadContext);

                var assemblies = new List<Assembly>(plugin.DllFiles.Count);
                var loadedAll = true;

                foreach (var file in plugin.DllFiles)
                {
                    try
                    {
                        assemblies.Add(assemblyLoadContext.LoadFromAssemblyPath(file));
                    }
                    catch (FileLoadException ex)
                    {
                        _logger.LogError(ex, "Failed to load assembly {Path}. Disabling plugin", file);
                        ChangePluginState(plugin, PluginStatus.Malfunctioned);
                        loadedAll = false;
                        break;
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        _logger.LogError(ex, "Failed to load assembly {Path}. Unknown exception was thrown. Disabling plugin", file);
                        ChangePluginState(plugin, PluginStatus.Malfunctioned);
                        loadedAll = false;
                        break;
                    }
                }

                if (!loadedAll)
                {
                    continue;
                }

                foreach (var assembly in assemblies)
                {
                    try
                    {
                        // Load all required types to verify that the plugin will load
                        assembly.GetTypes();
                    }
                    catch (SystemException ex) when (ex is TypeLoadException or ReflectionTypeLoadException) // Undocumented exception
                    {
                        _logger.LogError(ex, "Failed to load assembly {Path}. This error occurs when a plugin references an incompatible version of one of the shared libraries. Disabling plugin", assembly.Location);
                        ChangePluginState(plugin, PluginStatus.NotSupported);
                        break;
                    }
#pragma warning disable CA1031 // Do not catch general exception types
                    catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                    {
                        _logger.LogError(ex, "Failed to load assembly {Path}. Unknown exception was thrown. Disabling plugin", assembly.Location);
                        ChangePluginState(plugin, PluginStatus.Malfunctioned);
                        break;
                    }

                    _logger.LogInformation("Loaded assembly {Assembly} from {Path}", assembly.FullName, assembly.Location);
                    yield return assembly;
                }
            }
        }

        /// <summary>
        /// Creates all the plugin instances.
        /// </summary>
        public void CreatePlugins()
        {
            _ = _appHost.GetExports<IPlugin>(CreatePluginInstance);
        }

        /// <summary>
        /// Registers the plugin's services with the DI.
        /// Note: DI is not yet instantiated yet.
        /// </summary>
        /// <param name="serviceCollection">A <see cref="ServiceCollection"/> instance.</param>
        public void RegisterServices(IServiceCollection serviceCollection)
        {
            foreach (var pluginServiceRegistrator in _appHost.GetExportTypes<IPluginServiceRegistrator>())
            {
                var plugin = GetPluginByAssembly(pluginServiceRegistrator.Assembly);
                if (plugin is null)
                {
                    _logger.LogError("Unable to find plugin in assembly {Assembly}", pluginServiceRegistrator.Assembly.FullName);
                    continue;
                }

                UpdatePluginSupersededStatus(plugin);
                if (!plugin.IsEnabledAndSupported)
                {
                    continue;
                }

                try
                {
                    var instance = (IPluginServiceRegistrator?)Activator.CreateInstance(pluginServiceRegistrator);
                    instance?.RegisterServices(serviceCollection, _appHost);
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    _logger.LogError(ex, "Error registering plugin services from {Assembly}.", pluginServiceRegistrator.Assembly.FullName);
                    if (ChangePluginState(plugin, PluginStatus.Malfunctioned))
                    {
                        _logger.LogInformation("Disabling plugin {Path}", plugin.Path);
                    }
                }
            }
        }

        /// <summary>
        /// Imports a plugin manifest from <paramref name="folder"/>.
        /// </summary>
        /// <param name="folder">Folder of the plugin.</param>
        public void ImportPluginFrom(string folder)
        {
            ArgumentException.ThrowIfNullOrEmpty(folder);

            // Load the plugin.
            var plugin = LoadManifest(folder);
            // Make sure we haven't already loaded this.
            if (_plugins.Any(p => p.Manifest.Equals(plugin.Manifest)))
            {
                return;
            }

            _plugins.Add(plugin);
            EnablePlugin(plugin);
        }

        /// <summary>
        /// Removes the plugin reference '<paramref name="plugin"/>.
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        /// <returns>Outcome of the operation.</returns>
        public bool RemovePlugin(LocalPlugin plugin)
        {
            ArgumentNullException.ThrowIfNull(plugin);

            if (DeletePlugin(plugin))
            {
                ProcessAlternative(plugin);
                return true;
            }

            _logger.LogWarning("Unable to delete {Path}, so marking as deleteOnStartup.", plugin.Path);
            // Unable to delete, so disable.
            if (ChangePluginState(plugin, PluginStatus.Deleted))
            {
                ProcessAlternative(plugin);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to find the plugin with and id of <paramref name="id"/>.
        /// </summary>
        /// <param name="id">The <see cref="Guid"/> of plugin.</param>
        /// <param name="version">Optional <see cref="Version"/> of the plugin to locate.</param>
        /// <returns>A <see cref="LocalPlugin"/> if located, or null if not.</returns>
        public LocalPlugin? GetPlugin(Guid id, Version? version = null)
        {
            LocalPlugin? plugin;

            if (version is null)
            {
                // If no version is given, return the current instance.
                var plugins = _plugins.Where(p => p.Id.Equals(id)).ToList();

                plugin = plugins.FirstOrDefault(p => p.Instance is not null) ?? plugins.MaxBy(p => p.Version);
            }
            else
            {
                // Match id and version number.
                plugin = _plugins.FirstOrDefault(p => p.Id.Equals(id) && p.Version.Equals(version));
            }

            return plugin;
        }

        /// <summary>
        /// Enables the plugin, disabling all other versions.
        /// </summary>
        /// <param name="plugin">The <see cref="LocalPlugin"/> of the plug to disable.</param>
        public void EnablePlugin(LocalPlugin plugin)
        {
            ArgumentNullException.ThrowIfNull(plugin);

            if (ChangePluginState(plugin, PluginStatus.Active))
            {
                // See if there is another version, and if so, supercede it.
                ProcessAlternative(plugin);
            }
        }

        /// <summary>
        /// Disable the plugin.
        /// </summary>
        /// <param name="plugin">The <see cref="LocalPlugin"/> of the plug to disable.</param>
        public void DisablePlugin(LocalPlugin plugin)
        {
            ArgumentNullException.ThrowIfNull(plugin);

            // Update the manifest on disk
            if (ChangePluginState(plugin, PluginStatus.Disabled))
            {
                // If there is another version, activate it.
                ProcessAlternative(plugin);
            }
        }

        /// <summary>
        /// Disable the plugin.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> of the plug to disable.</param>
        public void FailPlugin(Assembly assembly)
        {
            // Only save if disabled.
            ArgumentNullException.ThrowIfNull(assembly);

            var plugin = _plugins.FirstOrDefault(p => p.DllFiles.Contains(assembly.Location));
            if (plugin is null)
            {
                // A plugin's assembly didn't cause this issue, so ignore it.
                return;
            }

            ChangePluginState(plugin, PluginStatus.Malfunctioned);
        }

        /// <inheritdoc/>
        public bool SaveManifest(PluginManifest manifest, string path)
        {
            try
            {
                var data = JsonSerializer.Serialize(manifest, _jsonOptions);
                File.WriteAllText(Path.Combine(path, MetafileName), data);
                return true;
            }
            catch (ArgumentException e)
            {
                _logger.LogWarning(e, "Unable to save plugin manifest due to invalid value. {Path}", path);
                return false;
            }
        }

        /// <inheritdoc/>
        public async Task<bool> PopulateManifest(PackageInfo packageInfo, Version version, string path, PluginStatus status)
        {
            var versionInfo = packageInfo.Versions.First(v => v.Version == version.ToString());
            var imagePath = string.Empty;

            if (!string.IsNullOrEmpty(packageInfo.ImageUrl))
            {
                var url = new Uri(packageInfo.ImageUrl);
                imagePath = Path.Join(path, url.Segments[^1]);

                var fileStream = AsyncFile.OpenWrite(imagePath);
                Stream? downloadStream = null;
                try
                {
                    downloadStream = await HttpClientFactory
                        .CreateClient(NamedClient.Default)
                        .GetStreamAsync(url)
                        .ConfigureAwait(false);

                    await downloadStream.CopyToAsync(fileStream).ConfigureAwait(false);
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "Failed to download image to path {Path} on disk.", imagePath);
                    imagePath = string.Empty;
                }
                finally
                {
                    await fileStream.DisposeAsync().ConfigureAwait(false);
                    if (downloadStream is not null)
                    {
                        await downloadStream.DisposeAsync().ConfigureAwait(false);
                    }
                }
            }

            var manifest = new PluginManifest
            {
                Category = packageInfo.Category,
                Changelog = versionInfo.Changelog ?? string.Empty,
                Description = packageInfo.Description,
                Id = packageInfo.Id,
                Name = packageInfo.Name,
                Overview = packageInfo.Overview,
                Owner = packageInfo.Owner,
                TargetAbi = versionInfo.TargetAbi ?? string.Empty,
                Timestamp = string.IsNullOrEmpty(versionInfo.Timestamp) ? DateTime.MinValue : DateTime.Parse(versionInfo.Timestamp, CultureInfo.InvariantCulture),
                Version = versionInfo.Version,
                Status = status == PluginStatus.Disabled ? PluginStatus.Disabled : PluginStatus.Active, // Keep disabled state.
                AutoUpdate = true,
                ImagePath = imagePath
            };

            if (!await ReconcileManifest(manifest, path).ConfigureAwait(false))
            {
                // An error occurred during reconciliation and saving could be undesirable.
                return false;
            }

            return SaveManifest(manifest, path);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var assemblyLoadContext in _assemblyLoadContexts)
            {
                assemblyLoadContext.Unload();
            }
        }

        /// <summary>
        /// Reconciles the manifest against any properties that exist locally in a pre-packaged meta.json found at the path.
        /// If no file is found, no reconciliation occurs.
        /// </summary>
        /// <param name="manifest">The <see cref="PluginManifest"/> to reconcile against.</param>
        /// <param name="path">The plugin path.</param>
        /// <returns>The reconciled <see cref="PluginManifest"/>.</returns>
        private async Task<bool> ReconcileManifest(PluginManifest manifest, string path)
        {
            try
            {
                var metafile = Path.Combine(path, MetafileName);
                if (!File.Exists(metafile))
                {
                    _logger.LogInformation("No local manifest exists for plugin {Plugin}. Skipping manifest reconciliation.", manifest.Name);
                    return true;
                }

                using var metaStream = File.OpenRead(metafile);
                var localManifest = await JsonSerializer.DeserializeAsync<PluginManifest>(metaStream, _jsonOptions).ConfigureAwait(false);
                localManifest ??= new PluginManifest();

                if (!Equals(localManifest.Id, manifest.Id))
                {
                    _logger.LogError("The manifest ID {LocalUUID} did not match the package info ID {PackageUUID}.", localManifest.Id, manifest.Id);
                    manifest.Status = PluginStatus.Malfunctioned;
                }

                if (localManifest.Version != manifest.Version)
                {
                    // Package information provides the version and is the source of truth. Pre-packages meta.json is assumed to be a mistake in this regard.
                    _logger.LogWarning("The version of the local manifest was {LocalVersion}, but {PackageVersion} was expected. The value will be replaced.", localManifest.Version, manifest.Version);
                }

                // Explicitly mapping properties instead of using reflection is preferred here.
                manifest.Category = string.IsNullOrEmpty(localManifest.Category) ? manifest.Category : localManifest.Category;
                manifest.AutoUpdate = localManifest.AutoUpdate; // Preserve whatever is local. Package info does not have this property.
                manifest.Changelog = string.IsNullOrEmpty(localManifest.Changelog) ? manifest.Changelog : localManifest.Changelog;
                manifest.Description = string.IsNullOrEmpty(localManifest.Description) ? manifest.Description : localManifest.Description;
                manifest.Name = string.IsNullOrEmpty(localManifest.Name) ? manifest.Name : localManifest.Name;
                manifest.Overview = string.IsNullOrEmpty(localManifest.Overview) ? manifest.Overview : localManifest.Overview;
                manifest.Owner = string.IsNullOrEmpty(localManifest.Owner) ? manifest.Owner : localManifest.Owner;
                manifest.TargetAbi = string.IsNullOrEmpty(localManifest.TargetAbi) ? manifest.TargetAbi : localManifest.TargetAbi;
                manifest.Timestamp = localManifest.Timestamp.Equals(default) ? manifest.Timestamp : localManifest.Timestamp;
                manifest.ImagePath = string.IsNullOrEmpty(localManifest.ImagePath) ? manifest.ImagePath : localManifest.ImagePath;
                manifest.Assemblies = localManifest.Assemblies;

                return true;
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Unable to reconcile plugin manifest due to an error. {Path}", path);
                return false;
            }
        }

        /// <summary>
        /// Changes a plugin's load status.
        /// </summary>
        /// <param name="plugin">The <see cref="LocalPlugin"/> instance.</param>
        /// <param name="state">The <see cref="PluginStatus"/> of the plugin.</param>
        /// <returns>Success of the task.</returns>
        private bool ChangePluginState(LocalPlugin plugin, PluginStatus state)
        {
            if (plugin.Manifest.Status == state || string.IsNullOrEmpty(plugin.Path))
            {
                // No need to save as the state hasn't changed.
                return true;
            }

            plugin.Manifest.Status = state;
            return SaveManifest(plugin.Manifest, plugin.Path);
        }

        /// <summary>
        /// Finds the plugin record using the assembly.
        /// </summary>
        /// <param name="assembly">The <see cref="Assembly"/> being sought.</param>
        /// <returns>The matching record, or null if not found.</returns>
        private LocalPlugin? GetPluginByAssembly(Assembly assembly)
        {
            // Find which plugin it is by the path.
            return _plugins.FirstOrDefault(p => p.DllFiles.Contains(assembly.Location, StringComparer.Ordinal));
        }

        /// <summary>
        /// Creates the instance safe.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        private IPlugin? CreatePluginInstance(Type type)
        {
            // Find the record for this plugin.
            var plugin = GetPluginByAssembly(type.Assembly);
            if (plugin?.Manifest.Status < PluginStatus.Active)
            {
                return null;
            }

            try
            {
                _logger.LogDebug("Creating instance of {Type}", type);
                // _appHost.ServiceProvider is already assigned when we create the plugins
                var instance = (IPlugin)ActivatorUtilities.CreateInstance(_appHost.ServiceProvider!, type);
                if (plugin is null)
                {
                    // Create a dummy record for the providers.
                    // TODO: remove this code once all provided have been released as separate plugins.
                    plugin = new LocalPlugin(
                        instance.AssemblyFilePath,
                        true,
                        new PluginManifest
                        {
                            Id = instance.Id,
                            Status = PluginStatus.Active,
                            Name = instance.Name,
                            Version = instance.Version.ToString()
                        })
                    {
                        Instance = instance
                    };

                    _plugins.Add(plugin);

                    plugin.Manifest.Status = PluginStatus.Active;
                }
                else
                {
                    plugin.Instance = instance;
                    var manifest = plugin.Manifest;
                    var pluginStr = instance.Version.ToString();
                    bool changed = false;
                    if (string.Equals(manifest.Version, pluginStr, StringComparison.Ordinal)
                        || !manifest.Id.Equals(instance.Id))
                    {
                        // If a plugin without a manifest failed to load due to an external issue (eg config),
                        // this updates the manifest to the actual plugin values.
                        manifest.Version = pluginStr;
                        manifest.Name = plugin.Instance.Name;
                        manifest.Description = plugin.Instance.Description;
                        manifest.Id = plugin.Instance.Id;
                        changed = true;
                    }

                    changed = changed || manifest.Status != PluginStatus.Active;
                    manifest.Status = PluginStatus.Active;

                    if (changed)
                    {
                        SaveManifest(manifest, plugin.Path);
                    }
                }

                _logger.LogInformation("Loaded plugin: {PluginName} {PluginVersion}", plugin.Name, plugin.Version);

                return instance;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                _logger.LogError(ex, "Error creating {Type}", type.FullName);
                if (plugin is not null)
                {
                    if (ChangePluginState(plugin, PluginStatus.Malfunctioned))
                    {
                        _logger.LogInformation("Plugin {Path} has been disabled.", plugin.Path);
                        return null;
                    }
                }

                _logger.LogDebug("Unable to auto-disable.");
                return null;
            }
        }

        private void UpdatePluginSupersededStatus(LocalPlugin plugin)
        {
            if (plugin.Manifest.Status != PluginStatus.Superseded)
            {
                return;
            }

            var predecessor = _plugins.OrderByDescending(p => p.Version)
                .FirstOrDefault(p => p.Id.Equals(plugin.Id) && p.IsEnabledAndSupported && p.Version != plugin.Version);
            if (predecessor is not null)
            {
                return;
            }

            plugin.Manifest.Status = PluginStatus.Active;
        }

        /// <summary>
        /// Attempts to delete a plugin.
        /// </summary>
        /// <param name="plugin">A <see cref="LocalPlugin"/> instance to delete.</param>
        /// <returns>True if successful.</returns>
        private bool DeletePlugin(LocalPlugin plugin)
        {
            // Attempt a cleanup of old folders.
            try
            {
                Directory.Delete(plugin.Path, true);
                _logger.LogDebug("Deleted {Path}", plugin.Path);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                return false;
            }

            return _plugins.Remove(plugin);
        }

        internal LocalPlugin LoadManifest(string dir)
        {
            Version? version;
            PluginManifest? manifest = null;
            var metafile = Path.Combine(dir, MetafileName);
            if (File.Exists(metafile))
            {
                // Only path where this stays null is when File.ReadAllBytes throws an IOException
                byte[] data = null!;
                try
                {
                    data = File.ReadAllBytes(metafile);
                    manifest = JsonSerializer.Deserialize<PluginManifest>(data, _jsonOptions);
                }
                catch (IOException ex)
                {
                    _logger.LogError(ex, "Error reading file {Path}.", dir);
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Error deserializing {Json}.", Encoding.UTF8.GetString(data));
                }

                if (manifest is not null)
                {
                    if (!Version.TryParse(manifest.TargetAbi, out var targetAbi))
                    {
                        targetAbi = _minimumVersion;
                    }

                    if (!Version.TryParse(manifest.Version, out version))
                    {
                        manifest.Version = _minimumVersion.ToString();
                    }

                    return new LocalPlugin(dir, _appVersion >= targetAbi, manifest);
                }
            }

            // No metafile, so lets see if the folder is versioned.
            // TODO: Phase this support out in future versions.
            metafile = dir.Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)[^1];
            int versionIndex = dir.LastIndexOf('_');
            if (versionIndex != -1)
            {
                // Get the version number from the filename if possible.
                metafile = Path.GetFileName(dir[..versionIndex]);
                version = Version.TryParse(dir.AsSpan()[(versionIndex + 1)..], out Version? parsedVersion) ? parsedVersion : _appVersion;
            }
            else
            {
                // Un-versioned folder - Add it under the path name and version it suitable for this instance.
                version = _appVersion;
            }

            // Auto-create a plugin manifest, so we can disable it, if it fails to load.
            manifest = new PluginManifest
            {
                Status = PluginStatus.Active,
                Name = metafile,
                AutoUpdate = false,
                Id = metafile.GetMD5(),
                TargetAbi = _appVersion.ToString(),
                Version = version.ToString()
            };

            return new LocalPlugin(dir, true, manifest);
        }

        /// <summary>
        /// Gets the list of local plugins.
        /// </summary>
        /// <returns>Enumerable of local plugins.</returns>
        private IEnumerable<LocalPlugin> DiscoverPlugins()
        {
            var versions = new List<LocalPlugin>();

            if (!Directory.Exists(_pluginsPath))
            {
                // Plugin path doesn't exist, don't try to enumerate sub-folders.
                return Enumerable.Empty<LocalPlugin>();
            }

            var directories = Directory.EnumerateDirectories(_pluginsPath, "*.*", SearchOption.TopDirectoryOnly);
            foreach (var dir in directories)
            {
                versions.Add(LoadManifest(dir));
            }

            string lastName = string.Empty;
            versions.Sort(LocalPlugin.Compare);
            // Traverse backwards through the list.
            // The first item will be the latest version.
            for (int x = versions.Count - 1; x >= 0; x--)
            {
                var entry = versions[x];
                if (!string.Equals(lastName, entry.Name, StringComparison.OrdinalIgnoreCase))
                {
                    if (!TryGetPluginDlls(entry, out var allowedDlls))
                    {
                        _logger.LogError("One or more assembly paths was invalid. Marking plugin {Plugin} as \"Malfunctioned\".", entry.Name);
                        ChangePluginState(entry, PluginStatus.Malfunctioned);
                        continue;
                    }

                    entry.DllFiles = allowedDlls;

                    if (entry.IsEnabledAndSupported)
                    {
                        lastName = entry.Name;
                        continue;
                    }
                }

                if (string.IsNullOrEmpty(lastName))
                {
                    continue;
                }

                var cleaned = false;
                var path = entry.Path;
                // Attempt a cleanup of old folders.
                try
                {
                    _logger.LogDebug("Deleting {Path}", path);
                    Directory.Delete(path, true);
                    cleaned = true;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    _logger.LogWarning(e, "Unable to delete {Path}", path);
                }

                if (cleaned)
                {
                    versions.RemoveAt(x);
                }
                else
                {
                    ChangePluginState(entry, PluginStatus.Deleted);
                }
            }

            // Only want plugin folders which have files.
            return versions.Where(p => p.DllFiles.Count != 0);
        }

        /// <summary>
        /// Attempts to retrieve valid DLLs from the plugin path. This method will consider the assembly whitelist
        /// from the manifest.
        /// </summary>
        /// <remarks>
        /// Loading DLLs from externally supplied paths introduces a path traversal risk. This method
        /// uses a safelisting tactic of considering DLLs from the plugin directory and only using
        /// the plugin's canonicalized assembly whitelist for comparison. See
        /// <see href="https://owasp.org/www-community/attacks/Path_Traversal"/> for more details.
        /// </remarks>
        /// <param name="plugin">The plugin.</param>
        /// <param name="whitelistedDlls">The whitelisted DLLs. If the method returns <see langword="false"/>, this will be empty.</param>
        /// <returns>
        /// <see langword="true"/> if all assemblies listed in the manifest were available in the plugin directory.
        /// <see langword="false"/> if any assemblies were invalid or missing from the plugin directory.
        /// </returns>
        /// <exception cref="ArgumentNullException">If the <see cref="LocalPlugin"/> is null.</exception>
        private bool TryGetPluginDlls(LocalPlugin plugin, out IReadOnlyList<string> whitelistedDlls)
        {
            ArgumentNullException.ThrowIfNull(plugin);

            IReadOnlyList<string> pluginDlls = Directory.GetFiles(plugin.Path, "*.dll", SearchOption.AllDirectories);

            whitelistedDlls = Array.Empty<string>();
            if (pluginDlls.Count > 0 && plugin.Manifest.Assemblies.Count > 0)
            {
                _logger.LogInformation("Registering whitelisted assemblies for plugin \"{Plugin}\"...", plugin.Name);

                var canonicalizedPaths = new List<string>();
                foreach (var path in plugin.Manifest.Assemblies)
                {
                    var canonicalized = Path.Combine(plugin.Path, path).Canonicalize();

                    // Ensure we stay in the plugin directory.
                    if (!canonicalized.StartsWith(plugin.Path.NormalizePath(), StringComparison.Ordinal))
                    {
                        _logger.LogError("Assembly path {Path} is not inside the plugin directory.", path);
                        return false;
                    }

                    canonicalizedPaths.Add(canonicalized);
                }

                var intersected = pluginDlls.Intersect(canonicalizedPaths).ToList();

                if (intersected.Count != canonicalizedPaths.Count)
                {
                    _logger.LogError("Plugin {Plugin} contained assembly paths that were not found in the directory.", plugin.Name);
                    return false;
                }

                whitelistedDlls = intersected;
            }
            else
            {
                // No whitelist, default to loading all DLLs in plugin directory.
                whitelistedDlls = pluginDlls;
            }

            return true;
        }

        /// <summary>
        /// Changes the status of the other versions of the plugin to "Superseded".
        /// </summary>
        /// <param name="plugin">The <see cref="LocalPlugin"/> that's master.</param>
        private void ProcessAlternative(LocalPlugin plugin)
        {
            // Detect whether there is another version of this plugin that needs disabling.
            var previousVersion = _plugins.OrderByDescending(p => p.Version)
                .FirstOrDefault(
                    p => p.Id.Equals(plugin.Id)
                    && p.IsEnabledAndSupported
                    && p.Version != plugin.Version);

            if (previousVersion is null)
            {
                // This value is memory only - so that the web will show restart required.
                plugin.Manifest.Status = PluginStatus.Restart;
                plugin.Manifest.AutoUpdate = false;
                return;
            }

            if (plugin.Manifest.Status == PluginStatus.Active && !ChangePluginState(previousVersion, PluginStatus.Superseded))
            {
                _logger.LogError("Unable to enable version {Version} of {Name}", previousVersion.Version, previousVersion.Name);
            }
            else if (plugin.Manifest.Status == PluginStatus.Superseded && !ChangePluginState(previousVersion, PluginStatus.Active))
            {
                _logger.LogError("Unable to supercede version {Version} of {Name}", previousVersion.Version, previousVersion.Name);
            }

            // This value is memory only - so that the web will show restart required.
            plugin.Manifest.Status = PluginStatus.Restart;
            plugin.Manifest.AutoUpdate = false;
        }
    }
}

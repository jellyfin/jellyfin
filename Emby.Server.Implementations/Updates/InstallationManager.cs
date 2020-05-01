using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Updates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Updates
{
    /// <summary>
    /// Manages all install, uninstall, and update operations for the system and individual plugins.
    /// </summary>
    public class InstallationManager : IInstallationManager
    {
        /// <summary>
        /// The key for a setting that specifies a URL for the plugin repository JSON manifest.
        /// </summary>
        public const string PluginManifestUrlKey = "InstallationManager:PluginManifestUrl";

        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger _logger;
        private readonly IApplicationPaths _appPaths;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Gets the application host.
        /// </summary>
        /// <value>The application host.</value>
        private readonly IApplicationHost _applicationHost;

        private readonly IZipClient _zipClient;
        private readonly IConfiguration _appConfig;

        private readonly object _currentInstallationsLock = new object();

        /// <summary>
        /// The current installations.
        /// </summary>
        private readonly List<(InstallationInfo info, CancellationTokenSource token)> _currentInstallations;

        /// <summary>
        /// The completed installations.
        /// </summary>
        private readonly ConcurrentBag<InstallationInfo> _completedInstallationsInternal;

        public InstallationManager(
            ILogger<InstallationManager> logger,
            IApplicationHost appHost,
            IApplicationPaths appPaths,
            IHttpClient httpClient,
            IJsonSerializer jsonSerializer,
            IServerConfigurationManager config,
            IFileSystem fileSystem,
            IZipClient zipClient,
            IConfiguration appConfig)
        {
            if (logger == null)
            {
                throw new ArgumentNullException(nameof(logger));
            }

            _currentInstallations = new List<(InstallationInfo, CancellationTokenSource)>();
            _completedInstallationsInternal = new ConcurrentBag<InstallationInfo>();

            _logger = logger;
            _applicationHost = appHost;
            _appPaths = appPaths;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _config = config;
            _fileSystem = fileSystem;
            _zipClient = zipClient;
            _appConfig = appConfig;
        }

        /// <inheritdoc />
        public event EventHandler<InstallationEventArgs> PackageInstalling;

        /// <inheritdoc />
        public event EventHandler<InstallationEventArgs> PackageInstallationCompleted;

        /// <inheritdoc />
        public event EventHandler<InstallationFailedEventArgs> PackageInstallationFailed;

        /// <inheritdoc />
        public event EventHandler<InstallationEventArgs> PackageInstallationCancelled;

        /// <inheritdoc />
        public event EventHandler<GenericEventArgs<IPlugin>> PluginUninstalled;

        /// <inheritdoc />
        public event EventHandler<GenericEventArgs<(IPlugin, VersionInfo)>> PluginUpdated;

        /// <inheritdoc />
        public event EventHandler<GenericEventArgs<VersionInfo>> PluginInstalled;

        /// <inheritdoc />
        public IEnumerable<InstallationInfo> CompletedInstallations => _completedInstallationsInternal;

        /// <inheritdoc />
        public async Task<IReadOnlyList<PackageInfo>> GetAvailablePackages(CancellationToken cancellationToken = default)
        {
            var manifestUrl = _appConfig.GetValue<string>(PluginManifestUrlKey);

            try
            {
                using (var response = await _httpClient.SendAsync(
                    new HttpRequestOptions
                    {
                        Url = manifestUrl,
                        CancellationToken = cancellationToken,
                        CacheMode = CacheMode.Unconditional,
                        CacheLength = TimeSpan.FromMinutes(3)
                    },
                    HttpMethod.Get).ConfigureAwait(false))
                using (Stream stream = response.Content)
                {
                    try
                    {
                        return await _jsonSerializer.DeserializeFromStreamAsync<IReadOnlyList<PackageInfo>>(stream).ConfigureAwait(false);
                    }
                    catch (SerializationException ex)
                    {
                        const string LogTemplate =
                            "Failed to deserialize the plugin manifest retrieved from {PluginManifestUrl}. If you " +
                            "have specified a custom plugin repository manifest URL with --plugin-manifest-url or " +
                            PluginManifestUrlKey + ", please ensure that it is correct.";
                        _logger.LogError(ex, LogTemplate, manifestUrl);
                        throw;
                    }
                }
            }
            catch (UriFormatException ex)
            {
                const string LogTemplate =
                    "The URL configured for the plugin repository manifest URL is not valid: {PluginManifestUrl}. " +
                    "Please check the URL configured by --plugin-manifest-url or " + PluginManifestUrlKey;
                _logger.LogError(ex, LogTemplate, manifestUrl);
                throw;
            }
        }

        /// <inheritdoc />
        public IEnumerable<PackageInfo> FilterPackages(
            IEnumerable<PackageInfo> availablePackages,
            string name = null,
            Guid guid = default)
        {
            if (name != null)
            {
                availablePackages = availablePackages.Where(x => x.name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }

            if (guid != Guid.Empty)
            {
                availablePackages = availablePackages.Where(x => Guid.Parse(x.guid) == guid);
            }

            return availablePackages;
        }

        /// <inheritdoc />
        public IEnumerable<VersionInfo> GetCompatibleVersions(
            IEnumerable<VersionInfo> availableVersions,
            Version minVersion = null)
        {
            var appVer = _applicationHost.ApplicationVersion;
            availableVersions = availableVersions
                .Where(x => Version.Parse(x.targetAbi) <= appVer);

            if (minVersion != null)
            {
                availableVersions = availableVersions.Where(x => x.version >= minVersion);
            }

            return availableVersions.OrderByDescending(x => x.version);
        }

        /// <inheritdoc />
        public IEnumerable<VersionInfo> GetCompatibleVersions(
            IEnumerable<PackageInfo> availablePackages,
            string name = null,
            Guid guid = default,
            Version minVersion = null)
        {
            var package = FilterPackages(availablePackages, name, guid).FirstOrDefault();

            // Package not found in repository
            if (package == null)
            {
                return Enumerable.Empty<VersionInfo>();
            }

            return GetCompatibleVersions(
                package.versions,
                minVersion);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<VersionInfo>> GetAvailablePluginUpdates(CancellationToken cancellationToken = default)
        {
            var catalog = await GetAvailablePackages(cancellationToken).ConfigureAwait(false);
            return GetAvailablePluginUpdates(catalog);
        }

        private IEnumerable<VersionInfo> GetAvailablePluginUpdates(IReadOnlyList<PackageInfo> pluginCatalog)
        {
            foreach (var plugin in _applicationHost.Plugins)
            {
                var compatibleversions = GetCompatibleVersions(pluginCatalog, plugin.Name, plugin.Id, plugin.Version);
                var version = compatibleversions.FirstOrDefault(y => y.version > plugin.Version);
                if (version != null && !CompletedInstallations.Any(x => string.Equals(x.Guid, version.guid, StringComparison.OrdinalIgnoreCase)))
                {
                    yield return version;
                }
            }
        }

        /// <inheritdoc />
        public async Task InstallPackage(VersionInfo package, CancellationToken cancellationToken)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            var installationInfo = new InstallationInfo
            {
                Guid = package.guid,
                Name = package.name,
                Version = package.version.ToString()
            };

            var innerCancellationTokenSource = new CancellationTokenSource();

            var tuple = (installationInfo, innerCancellationTokenSource);

            // Add it to the in-progress list
            lock (_currentInstallationsLock)
            {
                _currentInstallations.Add(tuple);
            }

            var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCancellationTokenSource.Token).Token;

            var installationEventArgs = new InstallationEventArgs
            {
                InstallationInfo = installationInfo,
                VersionInfo = package
            };

            PackageInstalling?.Invoke(this, installationEventArgs);

            try
            {
                await InstallPackageInternal(package, linkedToken).ConfigureAwait(false);

                lock (_currentInstallationsLock)
                {
                    _currentInstallations.Remove(tuple);
                }

                _completedInstallationsInternal.Add(installationInfo);

                PackageInstallationCompleted?.Invoke(this, installationEventArgs);
            }
            catch (OperationCanceledException)
            {
                lock (_currentInstallationsLock)
                {
                    _currentInstallations.Remove(tuple);
                }

                _logger.LogInformation("Package installation cancelled: {0} {1}", package.name, package.version);

                PackageInstallationCancelled?.Invoke(this, installationEventArgs);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Package installation failed");

                lock (_currentInstallationsLock)
                {
                    _currentInstallations.Remove(tuple);
                }

                PackageInstallationFailed?.Invoke(this, new InstallationFailedEventArgs
                {
                    InstallationInfo = installationInfo,
                    Exception = ex
                });

                throw;
            }
            finally
            {
                // Dispose the progress object and remove the installation from the in-progress list
                tuple.innerCancellationTokenSource.Dispose();
            }
        }

        /// <summary>
        /// Installs the package internal.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><see cref="Task" />.</returns>
        private async Task InstallPackageInternal(VersionInfo package, CancellationToken cancellationToken)
        {
            // Set last update time if we were installed before
            IPlugin plugin = _applicationHost.Plugins.FirstOrDefault(p => string.Equals(p.Id.ToString(), package.guid, StringComparison.OrdinalIgnoreCase))
                           ?? _applicationHost.Plugins.FirstOrDefault(p => p.Name.Equals(package.name, StringComparison.OrdinalIgnoreCase));

            // Do the install
            await PerformPackageInstallation(package, cancellationToken).ConfigureAwait(false);

            // Do plugin-specific processing
            if (plugin == null)
            {
                _logger.LogInformation("New plugin installed: {0} {1} {2}", package.name, package.version);

                PluginInstalled?.Invoke(this, new GenericEventArgs<VersionInfo>(package));
            }
            else
            {
                _logger.LogInformation("Plugin updated: {0} {1} {2}", package.name, package.version);

                PluginUpdated?.Invoke(this, new GenericEventArgs<(IPlugin, VersionInfo)>((plugin, package)));
            }

            _applicationHost.NotifyPendingRestart();
        }

        private async Task PerformPackageInstallation(VersionInfo package, CancellationToken cancellationToken)
        {
            var extension = Path.GetExtension(package.filename);
            if (!string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Only zip packages are supported. {Filename} is not a zip archive.", package.filename);
                return;
            }

            // Always override the passed-in target (which is a file) and figure it out again
            string targetDir = Path.Combine(_appPaths.PluginsPath, package.name);

            // CA5351: Do Not Use Broken Cryptographic Algorithms
#pragma warning disable CA5351
            using (var res = await _httpClient.SendAsync(
                new HttpRequestOptions
                {
                    Url = package.sourceUrl,
                    CancellationToken = cancellationToken,
                    // We need it to be buffered for setting the position
                    BufferContent = true
                },
                HttpMethod.Get).ConfigureAwait(false))
            using (var stream = res.Content)
            using (var md5 = MD5.Create())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var hash = Hex.Encode(md5.ComputeHash(stream));
                if (!string.Equals(package.checksum, hash, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError(
                        "The checksums didn't match while installing {Package}, expected: {Expected}, got: {Received}",
                        package.name,
                        package.checksum,
                        hash);
                    throw new InvalidDataException("The checksum of the received data doesn't match.");
                }

                if (Directory.Exists(targetDir))
                {
                    Directory.Delete(targetDir, true);
                }

                stream.Position = 0;
                _zipClient.ExtractAllFromZip(stream, targetDir, true);
            }

#pragma warning restore CA5351
        }

        /// <summary>
        /// Uninstalls a plugin.
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        public void UninstallPlugin(IPlugin plugin)
        {
            plugin.OnUninstalling();

            // Remove it the quick way for now
            _applicationHost.RemovePlugin(plugin);

            var path = plugin.AssemblyFilePath;
            bool isDirectory = false;
            // Check if we have a plugin directory we should remove too
            if (Path.GetDirectoryName(plugin.AssemblyFilePath) != _appPaths.PluginsPath)
            {
                path = Path.GetDirectoryName(plugin.AssemblyFilePath);
                isDirectory = true;
            }

            // Make this case-insensitive to account for possible incorrect assembly naming
            var file = _fileSystem.GetFilePaths(Path.GetDirectoryName(path))
                .FirstOrDefault(i => string.Equals(i, path, StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(file))
            {
                path = file;
            }

            if (isDirectory)
            {
                _logger.LogInformation("Deleting plugin directory {0}", path);
                Directory.Delete(path, true);
            }
            else
            {
                _logger.LogInformation("Deleting plugin file {0}", path);
                _fileSystem.DeleteFile(path);
            }

            var list = _config.Configuration.UninstalledPlugins.ToList();
            var filename = Path.GetFileName(path);
            if (!list.Contains(filename, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(filename);
                _config.Configuration.UninstalledPlugins = list.ToArray();
                _config.SaveConfiguration();
            }

            PluginUninstalled?.Invoke(this, new GenericEventArgs<IPlugin> { Argument = plugin });

            _applicationHost.NotifyPendingRestart();
        }

        /// <inheritdoc/>
        public bool CancelInstallation(Guid id)
        {
            lock (_currentInstallationsLock)
            {
                var install = _currentInstallations.Find(x => x.info.Guid == id.ToString());
                if (install == default((InstallationInfo, CancellationTokenSource)))
                {
                    return false;
                }

                install.token.Cancel();
                _currentInstallations.Remove(install);
                return true;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                lock (_currentInstallationsLock)
                {
                    foreach (var tuple in _currentInstallations)
                    {
                        tuple.token.Dispose();
                    }

                    _currentInstallations.Clear();
                }
            }
        }
    }
}

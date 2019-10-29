using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
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
using Microsoft.Extensions.Logging;
using static MediaBrowser.Common.HexHelper;

namespace Emby.Server.Implementations.Updates
{
    /// <summary>
    /// Manages all install, uninstall and update operations (both plugins and system)
    /// </summary>
    public class InstallationManager : IInstallationManager
    {
        public event EventHandler<InstallationEventArgs> PackageInstalling;
        public event EventHandler<InstallationEventArgs> PackageInstallationCompleted;
        public event EventHandler<InstallationFailedEventArgs> PackageInstallationFailed;
        public event EventHandler<InstallationEventArgs> PackageInstallationCancelled;

        /// <summary>
        /// The current installations
        /// </summary>
        private List<(InstallationInfo info, CancellationTokenSource token)> _currentInstallations { get; set; }

        /// <summary>
        /// The completed installations
        /// </summary>
        private ConcurrentBag<InstallationInfo> _completedInstallationsInternal;

        public IEnumerable<InstallationInfo> CompletedInstallations => _completedInstallationsInternal;

        /// <summary>
        /// Occurs when [plugin uninstalled].
        /// </summary>
        public event EventHandler<GenericEventArgs<IPlugin>> PluginUninstalled;

        /// <summary>
        /// Occurs when [plugin updated].
        /// </summary>
        public event EventHandler<GenericEventArgs<(IPlugin, PackageVersionInfo)>> PluginUpdated;

        /// <summary>
        /// Occurs when [plugin updated].
        /// </summary>
        public event EventHandler<GenericEventArgs<PackageVersionInfo>> PluginInstalled;

        /// <summary>
        /// The _logger
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

        public InstallationManager(
            ILogger<InstallationManager> logger,
            IApplicationHost appHost,
            IApplicationPaths appPaths,
            IHttpClient httpClient,
            IJsonSerializer jsonSerializer,
            IServerConfigurationManager config,
            IFileSystem fileSystem,
            IZipClient zipClient)
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
        }

        /// <summary>
        /// Gets all available packages.
        /// </summary>
        /// <returns>Task{List{PackageInfo}}.</returns>
        public async Task<List<PackageInfo>> GetAvailablePackages(
            CancellationToken cancellationToken,
            bool withRegistration = true,
            string packageType = null,
            Version applicationVersion = null)
        {
            var packages = await GetAvailablePackagesWithoutRegistrationInfo(cancellationToken).ConfigureAwait(false);
            return FilterPackages(packages, packageType, applicationVersion);
        }

        /// <summary>
        /// Gets all available packages.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{List{PackageInfo}}.</returns>
        public async Task<List<PackageInfo>> GetAvailablePackagesWithoutRegistrationInfo(CancellationToken cancellationToken)
        {
            using (var response = await _httpClient.SendAsync(
                new HttpRequestOptions
                {
                    Url = "https://repo.jellyfin.org/releases/plugin/manifest.json",
                    CancellationToken = cancellationToken,
                    CacheMode = CacheMode.Unconditional,
                    CacheLength = GetCacheLength()
                },
                HttpMethod.Get).ConfigureAwait(false))
            using (Stream stream = response.Content)
            {
                return FilterPackages(await _jsonSerializer.DeserializeFromStreamAsync<PackageInfo[]>(stream).ConfigureAwait(false));
            }
        }

        private static TimeSpan GetCacheLength()
        {
            return TimeSpan.FromMinutes(3);
        }

        protected List<PackageInfo> FilterPackages(IEnumerable<PackageInfo> packages)
        {
            var list = new List<PackageInfo>();

            foreach (var package in packages)
            {
                var versions = new List<PackageVersionInfo>();
                foreach (var version in package.versions)
                {
                    if (string.IsNullOrEmpty(version.sourceUrl))
                    {
                        continue;
                    }

                    versions.Add(version);
                }

                package.versions = versions
                    .OrderByDescending(x => x.Version)
                    .ToArray();

                if (package.versions.Length == 0)
                {
                    continue;
                }

                list.Add(package);
            }

            // Remove packages with no versions
            return list;
        }

        protected List<PackageInfo> FilterPackages(IEnumerable<PackageInfo> packages, string packageType, Version applicationVersion)
        {
            var packagesList = FilterPackages(packages);

            var returnList = new List<PackageInfo>();

            var filterOnPackageType = !string.IsNullOrEmpty(packageType);

            foreach (var p in packagesList)
            {
                if (filterOnPackageType && !string.Equals(p.type, packageType, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // If an app version was supplied, filter the versions for each package to only include supported versions
                if (applicationVersion != null)
                {
                    p.versions = p.versions.Where(v => IsPackageVersionUpToDate(v, applicationVersion)).ToArray();
                }

                if (p.versions.Length == 0)
                {
                    continue;
                }

                returnList.Add(p);
            }

            return returnList;
        }

        /// <summary>
        /// Determines whether [is package version up to date] [the specified package version info].
        /// </summary>
        /// <param name="packageVersionInfo">The package version info.</param>
        /// <param name="currentServerVersion">The current server version.</param>
        /// <returns><c>true</c> if [is package version up to date] [the specified package version info]; otherwise, <c>false</c>.</returns>
        private static bool IsPackageVersionUpToDate(PackageVersionInfo packageVersionInfo, Version currentServerVersion)
        {
            if (string.IsNullOrEmpty(packageVersionInfo.requiredVersionStr))
            {
                return true;
            }

            return Version.TryParse(packageVersionInfo.requiredVersionStr, out var requiredVersion) && currentServerVersion >= requiredVersion;
        }

        /// <summary>
        /// Gets the package.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="guid">The assembly guid</param>
        /// <param name="classification">The classification.</param>
        /// <param name="version">The version.</param>
        /// <returns>Task{PackageVersionInfo}.</returns>
        public async Task<PackageVersionInfo> GetPackage(string name, string guid, PackageVersionClass classification, Version version)
        {
            var packages = await GetAvailablePackages(CancellationToken.None, false).ConfigureAwait(false);

            var package = packages.FirstOrDefault(p => string.Equals(p.guid, guid ?? "none", StringComparison.OrdinalIgnoreCase))
                            ?? packages.FirstOrDefault(p => p.name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (package == null)
            {
                return null;
            }

            return package.versions.FirstOrDefault(v => v.Version == version && v.classification == classification);
        }

        /// <summary>
        /// Gets the latest compatible version.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="guid">The assembly guid if this is a plug-in</param>
        /// <param name="currentServerVersion">The current server version.</param>
        /// <param name="classification">The classification.</param>
        /// <returns>Task{PackageVersionInfo}.</returns>
        public async Task<PackageVersionInfo> GetLatestCompatibleVersion(string name, string guid, Version currentServerVersion, PackageVersionClass classification = PackageVersionClass.Release)
        {
            var packages = await GetAvailablePackages(CancellationToken.None, false).ConfigureAwait(false);

            return GetLatestCompatibleVersion(packages, name, guid, currentServerVersion, classification);
        }

        /// <summary>
        /// Gets the latest compatible version.
        /// </summary>
        /// <param name="availablePackages">The available packages.</param>
        /// <param name="name">The name.</param>
        /// <param name="currentServerVersion">The current server version.</param>
        /// <param name="classification">The classification.</param>
        /// <returns>PackageVersionInfo.</returns>
        public PackageVersionInfo GetLatestCompatibleVersion(IEnumerable<PackageInfo> availablePackages, string name, string guid, Version currentServerVersion, PackageVersionClass classification = PackageVersionClass.Release)
        {
            var package = availablePackages.FirstOrDefault(p => string.Equals(p.guid, guid ?? "none", StringComparison.OrdinalIgnoreCase))
                            ?? availablePackages.FirstOrDefault(p => p.name.Equals(name, StringComparison.OrdinalIgnoreCase));

            return package?.versions
                .OrderByDescending(x => x.Version)
                .FirstOrDefault(v => v.classification <= classification && IsPackageVersionUpToDate(v, currentServerVersion));
        }

        /// <summary>
        /// Gets the available plugin updates.
        /// </summary>
        /// <param name="applicationVersion">The current server version.</param>
        /// <param name="withAutoUpdateEnabled">if set to <c>true</c> [with auto update enabled].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{PackageVersionInfo}}.</returns>
        public async Task<IEnumerable<PackageVersionInfo>> GetAvailablePluginUpdates(Version applicationVersion, bool withAutoUpdateEnabled, CancellationToken cancellationToken)
        {
            var catalog = await GetAvailablePackagesWithoutRegistrationInfo(cancellationToken).ConfigureAwait(false);

            var systemUpdateLevel = _applicationHost.SystemUpdateLevel;

            // Figure out what needs to be installed
            return _applicationHost.Plugins.Select(p =>
            {
                var latestPluginInfo = GetLatestCompatibleVersion(catalog, p.Name, p.Id.ToString(), applicationVersion, systemUpdateLevel);

                return latestPluginInfo != null && latestPluginInfo.Version > p.Version ? latestPluginInfo : null;
            }).Where(i => i != null)
            .Where(p => !string.IsNullOrEmpty(p.sourceUrl) && !CompletedInstallations.Any(i => string.Equals(i.AssemblyGuid, p.guid, StringComparison.OrdinalIgnoreCase)));
        }

        /// <inheritdoc />
        public async Task InstallPackage(PackageVersionInfo package, CancellationToken cancellationToken)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            var installationInfo = new InstallationInfo
            {
                Id = Guid.NewGuid(),
                Name = package.name,
                AssemblyGuid = package.guid,
                UpdateClass = package.classification,
                Version = package.versionStr
            };

            var innerCancellationTokenSource = new CancellationTokenSource();

            var tuple = (installationInfo, innerCancellationTokenSource);

            // Add it to the in-progress list
            lock (_currentInstallations)
            {
                _currentInstallations.Add(tuple);
            }

            var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCancellationTokenSource.Token).Token;

            var installationEventArgs = new InstallationEventArgs
            {
                InstallationInfo = installationInfo,
                PackageVersionInfo = package
            };

            PackageInstalling?.Invoke(this, installationEventArgs);

            try
            {
                await InstallPackageInternal(package, linkedToken).ConfigureAwait(false);

                lock (_currentInstallations)
                {
                    _currentInstallations.Remove(tuple);
                }

                _completedInstallationsInternal.Add(installationInfo);

                PackageInstallationCompleted?.Invoke(this, installationEventArgs);
            }
            catch (OperationCanceledException)
            {
                lock (_currentInstallations)
                {
                    _currentInstallations.Remove(tuple);
                }

                _logger.LogInformation("Package installation cancelled: {0} {1}", package.name, package.versionStr);

                PackageInstallationCancelled?.Invoke(this, installationEventArgs);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Package installation failed");

                lock (_currentInstallations)
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
                tuple.Item2.Dispose();
            }
        }

        /// <summary>
        /// Installs the package internal.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns><see cref="Task" />.</returns>
        private async Task InstallPackageInternal(PackageVersionInfo package, CancellationToken cancellationToken)
        {
            // Set last update time if we were installed before
            IPlugin plugin = _applicationHost.Plugins.FirstOrDefault(p => string.Equals(p.Id.ToString(), package.guid, StringComparison.OrdinalIgnoreCase))
                           ?? _applicationHost.Plugins.FirstOrDefault(p => p.Name.Equals(package.name, StringComparison.OrdinalIgnoreCase));

            // Do the install
            await PerformPackageInstallation(package, cancellationToken).ConfigureAwait(false);

            // Do plugin-specific processing
            if (plugin == null)
            {
                _logger.LogInformation("New plugin installed: {0} {1} {2}", package.name, package.versionStr ?? string.Empty, package.classification);

                PluginInstalled?.Invoke(this, new GenericEventArgs<PackageVersionInfo>(package));
            }
            else
            {
                _logger.LogInformation("Plugin updated: {0} {1} {2}", package.name, package.versionStr ?? string.Empty, package.classification);

                PluginUpdated?.Invoke(this, new GenericEventArgs<(IPlugin, PackageVersionInfo)>((plugin, package)));
            }

            _applicationHost.NotifyPendingRestart();
        }

        private async Task PerformPackageInstallation(PackageVersionInfo package, CancellationToken cancellationToken)
        {
            var extension = Path.GetExtension(package.targetFilename);
            if (!string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Only zip packages are supported. {Filename} is not a zip archive.", package.targetFilename);
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

                var hash = ToHexString(md5.ComputeHash(stream));
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
        /// Uninstalls a plugin
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
            lock (_currentInstallations)
            {
                var install = _currentInstallations.Find(x => x.Item1.Id == id);
                if (install == default((InstallationInfo, CancellationTokenSource)))
                {
                    return false;
                }

                install.Item2.Cancel();
                _currentInstallations.Remove(install);
                return true;
            }
        }

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
                lock (_currentInstallations)
                {
                    foreach (var tuple in _currentInstallations)
                    {
                        tuple.Item2.Dispose();
                    }

                    _currentInstallations.Clear();
                }
            }
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Progress;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Updates;
using Microsoft.Extensions.Logging;

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
        public List<Tuple<InstallationInfo, CancellationTokenSource>> CurrentInstallations { get; set; }

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
        /// Called when [plugin uninstalled].
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        private void OnPluginUninstalled(IPlugin plugin)
        {
            PluginUninstalled?.Invoke(this, new GenericEventArgs<IPlugin> { Argument = plugin });
        }

        /// <summary>
        /// Occurs when [plugin updated].
        /// </summary>
        public event EventHandler<GenericEventArgs<Tuple<IPlugin, PackageVersionInfo>>> PluginUpdated;
        /// <summary>
        /// Called when [plugin updated].
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        /// <param name="newVersion">The new version.</param>
        private void OnPluginUpdated(IPlugin plugin, PackageVersionInfo newVersion)
        {
            _logger.LogInformation("Plugin updated: {0} {1} {2}", newVersion.name, newVersion.versionStr ?? string.Empty, newVersion.classification);

            PluginUpdated?.Invoke(this, new GenericEventArgs<Tuple<IPlugin, PackageVersionInfo>> { Argument = new Tuple<IPlugin, PackageVersionInfo>(plugin, newVersion) });

            _applicationHost.NotifyPendingRestart();
        }

        /// <summary>
        /// Occurs when [plugin updated].
        /// </summary>
        public event EventHandler<GenericEventArgs<PackageVersionInfo>> PluginInstalled;
        /// <summary>
        /// Called when [plugin installed].
        /// </summary>
        /// <param name="package">The package.</param>
        private void OnPluginInstalled(PackageVersionInfo package)
        {
            _logger.LogInformation("New plugin installed: {0} {1} {2}", package.name, package.versionStr ?? string.Empty, package.classification);

            PluginInstalled?.Invoke(this, new GenericEventArgs<PackageVersionInfo> { Argument = package });

            _applicationHost.NotifyPendingRestart();
        }

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
            ILoggerFactory loggerFactory,
            IApplicationHost appHost,
            IApplicationPaths appPaths,
            IHttpClient httpClient,
            IJsonSerializer jsonSerializer,
            IServerConfigurationManager config,
            IFileSystem fileSystem,
            IZipClient zipClient)
        {
            if (loggerFactory == null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            CurrentInstallations = new List<Tuple<InstallationInfo, CancellationTokenSource>>();
            _completedInstallationsInternal = new ConcurrentBag<InstallationInfo>();

            _logger = loggerFactory.CreateLogger(nameof(InstallationManager));
            _applicationHost = appHost;
            _appPaths = appPaths;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _config = config;
            _fileSystem = fileSystem;
            _zipClient = zipClient;
        }

        private static Version GetPackageVersion(PackageVersionInfo version)
        {
            return new Version(ValueOrDefault(version.versionStr, "0.0.0.1"));
        }

        private static string ValueOrDefault(string str, string def)
        {
            return string.IsNullOrEmpty(str) ? def : str;
        }

        /// <summary>
        /// Gets all available packages.
        /// </summary>
        /// <returns>Task{List{PackageInfo}}.</returns>
        public async Task<List<PackageInfo>> GetAvailablePackages(CancellationToken cancellationToken,
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
            using (var response = await _httpClient.SendAsync(new HttpRequestOptions
            {
                Url = "https://repo.jellyfin.org/releases/plugin/manifest.json",
                CancellationToken = cancellationToken,
                Progress = new SimpleProgress<double>(),
                CacheLength = GetCacheLength()
            }, "GET").ConfigureAwait(false))
            {
                using (var stream = response.Content)
                {
                    return FilterPackages(await _jsonSerializer.DeserializeFromStreamAsync<PackageInfo[]>(stream).ConfigureAwait(false));
                }
            }
        }

        private PackageVersionClass GetSystemUpdateLevel()
        {
            return _applicationHost.SystemUpdateLevel;
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
                    .OrderByDescending(GetPackageVersion)
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

            return package.versions.FirstOrDefault(v => GetPackageVersion(v).Equals(version) && v.classification == classification);
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

            if (package == null)
            {
                return null;
            }

            return package.versions
                .OrderByDescending(GetPackageVersion)
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

            var systemUpdateLevel = GetSystemUpdateLevel();

            // Figure out what needs to be installed
            return _applicationHost.Plugins.Select(p =>
            {
                var latestPluginInfo = GetLatestCompatibleVersion(catalog, p.Name, p.Id.ToString(), applicationVersion, systemUpdateLevel);

                return latestPluginInfo != null && GetPackageVersion(latestPluginInfo) > p.Version ? latestPluginInfo : null;

            }).Where(i => i != null)
            .Where(p => !string.IsNullOrEmpty(p.sourceUrl) && !CompletedInstallations.Any(i => string.Equals(i.AssemblyGuid, p.guid, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Installs the package.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="isPlugin">if set to <c>true</c> [is plugin].</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">package</exception>
        public async Task InstallPackage(PackageVersionInfo package, bool isPlugin, IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            if (progress == null)
            {
                throw new ArgumentNullException(nameof(progress));
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

            var tuple = new Tuple<InstallationInfo, CancellationTokenSource>(installationInfo, innerCancellationTokenSource);

            // Add it to the in-progress list
            lock (CurrentInstallations)
            {
                CurrentInstallations.Add(tuple);
            }

            var innerProgress = new ActionableProgress<double>();

            // Whenever the progress updates, update the outer progress object and InstallationInfo
            innerProgress.RegisterAction(percent =>
            {
                progress.Report(percent);

                installationInfo.PercentComplete = percent;
            });

            var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCancellationTokenSource.Token).Token;

            var installationEventArgs = new InstallationEventArgs
            {
                InstallationInfo = installationInfo,
                PackageVersionInfo = package
            };

            PackageInstalling?.Invoke(this, installationEventArgs);

            try
            {
                await InstallPackageInternal(package, isPlugin, innerProgress, linkedToken).ConfigureAwait(false);

                lock (CurrentInstallations)
                {
                    CurrentInstallations.Remove(tuple);
                }

                _completedInstallationsInternal.Add(installationInfo);

                PackageInstallationCompleted?.Invoke(this, installationEventArgs);
            }
            catch (OperationCanceledException)
            {
                lock (CurrentInstallations)
                {
                    CurrentInstallations.Remove(tuple);
                }

                _logger.LogInformation("Package installation cancelled: {0} {1}", package.name, package.versionStr);

                PackageInstallationCancelled?.Invoke(this, installationEventArgs);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Package installation failed");

                lock (CurrentInstallations)
                {
                    CurrentInstallations.Remove(tuple);
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
        /// <param name="isPlugin">if set to <c>true</c> [is plugin].</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task InstallPackageInternal(PackageVersionInfo package, bool isPlugin, IProgress<double> progress, CancellationToken cancellationToken)
        {
            IPlugin plugin = null;

            if (isPlugin)
            {
                // Set last update time if we were installed before
                plugin = _applicationHost.Plugins.FirstOrDefault(p => string.Equals(p.Id.ToString(), package.guid, StringComparison.OrdinalIgnoreCase))
                           ?? _applicationHost.Plugins.FirstOrDefault(p => p.Name.Equals(package.name, StringComparison.OrdinalIgnoreCase));
            }

            string targetPath = plugin == null ? null : plugin.AssemblyFilePath;

            // Do the install
            await PerformPackageInstallation(progress, targetPath, package, cancellationToken).ConfigureAwait(false);

            // Do plugin-specific processing
            if (isPlugin)
            {
                if (plugin == null)
                {
                    OnPluginInstalled(package);
                }
                else
                {
                    OnPluginUpdated(plugin, package);
                }
            }
        }

        private async Task PerformPackageInstallation(IProgress<double> progress, string target, PackageVersionInfo package, CancellationToken cancellationToken)
        {
            // TODO: Remove the `string target` argument as it is not used any longer

            var extension = Path.GetExtension(package.targetFilename);
            var isArchive = string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase);

            if (!isArchive)
            {
                _logger.LogError("Only zip packages are supported. {Filename} is not a zip archive.", package.targetFilename);
                return;
            }

            // Always override the passed-in target (which is a file) and figure it out again
            target = Path.Combine(_appPaths.PluginsPath, package.name);
            _logger.LogDebug("Installing plugin to {Filename}.", target);

            // Download to temporary file so that, if interrupted, it won't destroy the existing installation
            _logger.LogDebug("Downloading ZIP.");
            var tempFile = await _httpClient.GetTempFile(new HttpRequestOptions
            {
                Url = package.sourceUrl,
                CancellationToken = cancellationToken,
                Progress = progress

            }).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // TODO: Validate with a checksum, *properly*

            // Check if the target directory already exists, and remove it if so
            if (Directory.Exists(target))
            {
                _logger.LogDebug("Deleting existing plugin at {Filename}.", target);
                Directory.Delete(target, true);
            }

            // Success - move it to the real target
            try
            {
                _logger.LogDebug("Extracting ZIP {TempFile} to {Filename}.", tempFile, target);
                using (var stream = File.OpenRead(tempFile))
                {
                    _zipClient.ExtractAllFromZip(stream, target, true);
                }
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, "Error attempting to extract {TempFile} to {TargetFile}", tempFile, target);
                throw;
            }

            try
            {
                _logger.LogDebug("Deleting temporary file {Filename}.", tempFile);
                _fileSystem.DeleteFile(tempFile);
            }
            catch (IOException ex)
            {
                // Don't fail because of this
                _logger.LogError(ex, "Error deleting temp file {TempFile}", tempFile);
            }
        }

        /// <summary>
        /// Uninstalls a plugin
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        /// <exception cref="ArgumentException"></exception>
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

            OnPluginUninstalled(plugin);

            _applicationHost.NotifyPendingRestart();
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                lock (CurrentInstallations)
                {
                    foreach (var tuple in CurrentInstallations)
                    {
                        tuple.Item2.Dispose();
                    }

                    CurrentInstallations.Clear();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}

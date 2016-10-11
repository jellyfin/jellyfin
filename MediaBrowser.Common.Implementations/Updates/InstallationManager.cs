using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Implementations.Security;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Progress;
using MediaBrowser.Common.Security;
using MediaBrowser.Common.Updates;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Updates;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.Common.Implementations.Updates
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
        public ConcurrentBag<InstallationInfo> CompletedInstallations { get; set; }

        #region PluginUninstalled Event
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
            EventHelper.FireEventIfNotNull(PluginUninstalled, this, new GenericEventArgs<IPlugin> { Argument = plugin }, _logger);
        }
        #endregion

        #region PluginUpdated Event
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
            _logger.Info("Plugin updated: {0} {1} {2}", newVersion.name, newVersion.versionStr ?? string.Empty, newVersion.classification);

            EventHelper.FireEventIfNotNull(PluginUpdated, this, new GenericEventArgs<Tuple<IPlugin, PackageVersionInfo>> { Argument = new Tuple<IPlugin, PackageVersionInfo>(plugin, newVersion) }, _logger);

            _applicationHost.NotifyPendingRestart();
        }
        #endregion

        #region PluginInstalled Event
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
            _logger.Info("New plugin installed: {0} {1} {2}", package.name, package.versionStr ?? string.Empty, package.classification);

            EventHelper.FireEventIfNotNull(PluginInstalled, this, new GenericEventArgs<PackageVersionInfo> { Argument = package }, _logger);

            _applicationHost.NotifyPendingRestart();
        }
        #endregion

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        private readonly IApplicationPaths _appPaths;
        private readonly IHttpClient _httpClient;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly ISecurityManager _securityManager;
        private readonly IConfigurationManager _config;
        private readonly IFileSystem _fileSystem;

        /// <summary>
        /// Gets the application host.
        /// </summary>
        /// <value>The application host.</value>
        private readonly IApplicationHost _applicationHost;

        public InstallationManager(ILogger logger, IApplicationHost appHost, IApplicationPaths appPaths, IHttpClient httpClient, IJsonSerializer jsonSerializer, ISecurityManager securityManager, IConfigurationManager config, IFileSystem fileSystem)
        {
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }

            CurrentInstallations = new List<Tuple<InstallationInfo, CancellationTokenSource>>();
            CompletedInstallations = new ConcurrentBag<InstallationInfo>();

            _applicationHost = appHost;
            _appPaths = appPaths;
            _httpClient = httpClient;
            _jsonSerializer = jsonSerializer;
            _securityManager = securityManager;
            _config = config;
            _fileSystem = fileSystem;
            _logger = logger;
        }

        private Version GetPackageVersion(PackageVersionInfo version)
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
        public async Task<IEnumerable<PackageInfo>> GetAvailablePackages(CancellationToken cancellationToken,
            bool withRegistration = true,
            string packageType = null,
            Version applicationVersion = null)
        {
            var data = new Dictionary<string, string>
            {
                { "key", _securityManager.SupporterKey }, 
                { "mac", _applicationHost.SystemId }, 
                { "systemid", _applicationHost.SystemId }
            };

            if (withRegistration)
            {
                using (var json = await _httpClient.Post(MbAdmin.HttpsUrl + "service/package/retrieveall", data, cancellationToken).ConfigureAwait(false))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var packages = _jsonSerializer.DeserializeFromStream<List<PackageInfo>>(json).ToList();

                    return FilterPackages(packages, packageType, applicationVersion);
                }
            }
            else
            {
                var packages = await GetAvailablePackagesWithoutRegistrationInfo(cancellationToken).ConfigureAwait(false);

                return FilterPackages(packages.ToList(), packageType, applicationVersion);
            }
        }

        private DateTime _lastPackageUpdateTime;

        /// <summary>
        /// Gets all available packages.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{List{PackageInfo}}.</returns>
        public async Task<IEnumerable<PackageInfo>> GetAvailablePackagesWithoutRegistrationInfo(CancellationToken cancellationToken)
        {
            _logger.Info("Opening {0}", PackageCachePath);
            try
            {
                using (var stream = _fileSystem.OpenRead(PackageCachePath))
                {
                    var packages = _jsonSerializer.DeserializeFromStream<List<PackageInfo>>(stream).ToList();

                    if (DateTime.UtcNow - _lastPackageUpdateTime > GetCacheLength())
                    {
                        UpdateCachedPackages(CancellationToken.None, false);
                    }

                    return packages;
                }
            }
            catch (Exception)
            {

            }

            _lastPackageUpdateTime = DateTime.MinValue;
            await UpdateCachedPackages(cancellationToken, true).ConfigureAwait(false);
            using (var stream = _fileSystem.OpenRead(PackageCachePath))
            {
                return _jsonSerializer.DeserializeFromStream<List<PackageInfo>>(stream).ToList();
            }
        }

        private string PackageCachePath
        {
            get { return Path.Combine(_appPaths.CachePath, "serverpackages.json"); }
        }

        private readonly SemaphoreSlim _updateSemaphore = new SemaphoreSlim(1, 1);
        private async Task UpdateCachedPackages(CancellationToken cancellationToken, bool throwErrors)
        {
            await _updateSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (DateTime.UtcNow - _lastPackageUpdateTime < GetCacheLength())
                {
                    return;
                }

                var tempFile = await _httpClient.GetTempFile(new HttpRequestOptions
                {
                    Url = MbAdmin.HttpUrl + "service/MB3Packages.json",
                    CancellationToken = cancellationToken,
                    Progress = new Progress<Double>()

                }).ConfigureAwait(false);

                _fileSystem.CreateDirectory(Path.GetDirectoryName(PackageCachePath));

                _fileSystem.CopyFile(tempFile, PackageCachePath, true);
                _lastPackageUpdateTime = DateTime.UtcNow;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error updating package cache", ex);

                if (throwErrors)
                {
                    throw;
                }
            }
            finally
            {
                _updateSemaphore.Release();
            }
        }

        private TimeSpan GetCacheLength()
        {
            switch (_config.CommonConfiguration.SystemUpdateLevel)
            {
                case PackageVersionClass.Beta:
                    return TimeSpan.FromMinutes(30);
                case PackageVersionClass.Dev:
                    return TimeSpan.FromMinutes(3);
                default:
                    return TimeSpan.FromHours(24);
            }
        }

        protected IEnumerable<PackageInfo> FilterPackages(List<PackageInfo> packages)
        {
            foreach (var package in packages)
            {
                package.versions = package.versions.Where(v => !string.IsNullOrWhiteSpace(v.sourceUrl))
                    .OrderByDescending(GetPackageVersion).ToList();
            }

            // Remove packages with no versions
            packages = packages.Where(p => p.versions.Any()).ToList();

            return packages;
        }

        protected IEnumerable<PackageInfo> FilterPackages(List<PackageInfo> packages, string packageType, Version applicationVersion)
        {
            foreach (var package in packages)
            {
                package.versions = package.versions.Where(v => !string.IsNullOrWhiteSpace(v.sourceUrl))
                    .OrderByDescending(GetPackageVersion).ToList();
            }

            if (!string.IsNullOrWhiteSpace(packageType))
            {
                packages = packages.Where(p => string.Equals(p.type, packageType, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // If an app version was supplied, filter the versions for each package to only include supported versions
            if (applicationVersion != null)
            {
                foreach (var package in packages)
                {
                    package.versions = package.versions.Where(v => IsPackageVersionUpToDate(v, applicationVersion)).ToList();
                }
            }

            // Remove packages with no versions
            packages = packages.Where(p => p.versions.Any()).ToList();

            return packages;
        }

        /// <summary>
        /// Determines whether [is package version up to date] [the specified package version info].
        /// </summary>
        /// <param name="packageVersionInfo">The package version info.</param>
        /// <param name="currentServerVersion">The current server version.</param>
        /// <returns><c>true</c> if [is package version up to date] [the specified package version info]; otherwise, <c>false</c>.</returns>
        private bool IsPackageVersionUpToDate(PackageVersionInfo packageVersionInfo, Version currentServerVersion)
        {
            if (string.IsNullOrEmpty(packageVersionInfo.requiredVersionStr))
            {
                return true;
            }

            Version requiredVersion;

            return Version.TryParse(packageVersionInfo.requiredVersionStr, out requiredVersion) && currentServerVersion >= requiredVersion;
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
            var packages = await GetAvailablePackages(CancellationToken.None).ConfigureAwait(false);

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
            var packages = await GetAvailablePackages(CancellationToken.None).ConfigureAwait(false);

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

            var plugins = _applicationHost.Plugins.ToList();

            if (withAutoUpdateEnabled)
            {
                plugins = plugins
                    .Where(p => _config.CommonConfiguration.EnableAutoUpdate)
                    .ToList();
            }

            // Figure out what needs to be installed
            var packages = plugins.Select(p =>
            {
                var latestPluginInfo = GetLatestCompatibleVersion(catalog, p.Name, p.Id.ToString(), applicationVersion, _config.CommonConfiguration.SystemUpdateLevel);

                return latestPluginInfo != null && GetPackageVersion(latestPluginInfo) > p.Version ? latestPluginInfo : null;

            }).Where(i => i != null).ToList();

            return packages
                .Where(p => !string.IsNullOrWhiteSpace(p.sourceUrl) && !CompletedInstallations.Any(i => string.Equals(i.AssemblyGuid, p.guid, StringComparison.OrdinalIgnoreCase)));
        }

        /// <summary>
        /// Installs the package.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="isPlugin">if set to <c>true</c> [is plugin].</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">package</exception>
        public async Task InstallPackage(PackageVersionInfo package, bool isPlugin, IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            if (progress == null)
            {
                throw new ArgumentNullException("progress");
            }

            var installationInfo = new InstallationInfo
            {
                Id = Guid.NewGuid().ToString("N"),
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

            EventHelper.FireEventIfNotNull(PackageInstalling, this, installationEventArgs, _logger);

            try
            {
                await InstallPackageInternal(package, isPlugin, innerProgress, linkedToken).ConfigureAwait(false);

                lock (CurrentInstallations)
                {
                    CurrentInstallations.Remove(tuple);
                }

                progress.Report(100);

                CompletedInstallations.Add(installationInfo);

                EventHelper.FireEventIfNotNull(PackageInstallationCompleted, this, installationEventArgs, _logger);
            }
            catch (OperationCanceledException)
            {
                lock (CurrentInstallations)
                {
                    CurrentInstallations.Remove(tuple);
                }

                _logger.Info("Package installation cancelled: {0} {1}", package.name, package.versionStr);

                EventHelper.FireEventIfNotNull(PackageInstallationCancelled, this, installationEventArgs, _logger);

                throw;
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Package installation failed", ex);

                lock (CurrentInstallations)
                {
                    CurrentInstallations.Remove(tuple);
                }

                EventHelper.FireEventIfNotNull(PackageInstallationFailed, this, new InstallationFailedEventArgs
                {
                    InstallationInfo = installationInfo,
                    Exception = ex

                }, _logger);

                throw;
            }
            finally
            {
                // Dispose the progress object and remove the installation from the in-progress list
                innerProgress.Dispose();
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
            // Do the install
            await PerformPackageInstallation(progress, package, cancellationToken).ConfigureAwait(false);

            // Do plugin-specific processing
            if (isPlugin)
            {
                // Set last update time if we were installed before
                var plugin = _applicationHost.Plugins.FirstOrDefault(p => string.Equals(p.Id.ToString(), package.guid, StringComparison.OrdinalIgnoreCase))
                            ?? _applicationHost.Plugins.FirstOrDefault(p => p.Name.Equals(package.name, StringComparison.OrdinalIgnoreCase));

                if (plugin != null)
                {
                    OnPluginUpdated(plugin, package);
                }
                else
                {
                    OnPluginInstalled(package);
                }
            }
        }

        private async Task PerformPackageInstallation(IProgress<double> progress, PackageVersionInfo package, CancellationToken cancellationToken)
        {
            // Target based on if it is an archive or single assembly
            //  zip archives are assumed to contain directory structures relative to our ProgramDataPath
            var extension = Path.GetExtension(package.targetFilename);
            var isArchive = string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".rar", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".7z", StringComparison.OrdinalIgnoreCase);
            var target = Path.Combine(isArchive ? _appPaths.TempUpdatePath : _appPaths.PluginsPath, package.targetFilename);

            // Download to temporary file so that, if interrupted, it won't destroy the existing installation
            var tempFile = await _httpClient.GetTempFile(new HttpRequestOptions
            {
                Url = package.sourceUrl,
                CancellationToken = cancellationToken,
                Progress = progress

            }).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // Validate with a checksum
            var packageChecksum = string.IsNullOrWhiteSpace(package.checksum) ? Guid.Empty : new Guid(package.checksum);
            if (packageChecksum != Guid.Empty) // support for legacy uploads for now
            {
                using (var crypto = new MD5CryptoServiceProvider())
                using (var stream = new BufferedStream(_fileSystem.OpenRead(tempFile), 100000))
                {
                    var check = Guid.Parse(BitConverter.ToString(crypto.ComputeHash(stream)).Replace("-", String.Empty));
                    if (check != packageChecksum)
                    {
                        throw new ApplicationException(string.Format("Download validation failed for {0}.  Probably corrupted during transfer.", package.name));
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Success - move it to the real target 
            try
            {
                _fileSystem.CreateDirectory(Path.GetDirectoryName(target));
                _fileSystem.CopyFile(tempFile, target, true);
                //If it is an archive - write out a version file so we know what it is
                if (isArchive)
                {
                    File.WriteAllText(target + ".ver", package.versionStr);
                }
            }
            catch (IOException e)
            {
                _logger.ErrorException("Error attempting to move file from {0} to {1}", e, tempFile, target);
                throw;
            }

            try
            {
                _fileSystem.DeleteFile(tempFile);
            }
            catch (IOException e)
            {
                // Don't fail because of this
                _logger.ErrorException("Error deleting temp file {0]", e, tempFile);
            }
        }

        /// <summary>
        /// Uninstalls a plugin
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        /// <exception cref="System.ArgumentException"></exception>
        public void UninstallPlugin(IPlugin plugin)
        {
            plugin.OnUninstalling();

            // Remove it the quick way for now
            _applicationHost.RemovePlugin(plugin);

            _fileSystem.DeleteFile(plugin.AssemblyFilePath);

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

using MediaBrowser.Common;
using MediaBrowser.Common.Events;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Progress;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller.Updates;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Updates;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Updates
{
    /// <summary>
    /// Manages all install, uninstall and update operations (both plugins and system)
    /// </summary>
    public class InstallationManager : IInstallationManager
    {
        public event EventHandler<GenericEventArgs<InstallationInfo>> PackageInstalling;
        public event EventHandler<GenericEventArgs<InstallationInfo>> PackageInstallationCompleted;
        public event EventHandler<GenericEventArgs<InstallationInfo>> PackageInstallationFailed;
        public event EventHandler<GenericEventArgs<InstallationInfo>> PackageInstallationCancelled;

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
            EventHelper.QueueEventIfNotNull(PluginUninstalled, this, new GenericEventArgs<IPlugin> { Argument = plugin }, _logger);
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
            _logger.Info("Plugin updated: {0} {1} {2}", newVersion.name, newVersion.version, newVersion.classification);

            EventHelper.QueueEventIfNotNull(PluginUpdated, this, new GenericEventArgs<Tuple<IPlugin, PackageVersionInfo>> { Argument = new Tuple<IPlugin, PackageVersionInfo>(plugin, newVersion) }, _logger);

            ApplicationHost.NotifyPendingRestart();
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
            _logger.Info("New plugin installed: {0} {1} {2}", package.name, package.version, package.classification);

            EventHelper.QueueEventIfNotNull(PluginInstalled, this, new GenericEventArgs<PackageVersionInfo> { Argument = package }, _logger);

            ApplicationHost.NotifyPendingRestart();
        }
        #endregion

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The package manager
        /// </summary>
        private readonly IPackageManager _packageManager;

        /// <summary>
        /// Gets the json serializer.
        /// </summary>
        /// <value>The json serializer.</value>
        protected IJsonSerializer JsonSerializer { get; private set; }

        /// <summary>
        /// Gets the HTTP client.
        /// </summary>
        /// <value>The HTTP client.</value>
        protected IHttpClient HttpClient { get; private set; }

        /// <summary>
        /// Gets the application host.
        /// </summary>
        /// <value>The application host.</value>
        protected IApplicationHost ApplicationHost { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InstallationManager" /> class.
        /// </summary>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="packageManager">The package manager.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="appHost">The app host.</param>
        /// <exception cref="System.ArgumentNullException">zipClient</exception>
        public InstallationManager(IHttpClient httpClient, IPackageManager packageManager, IJsonSerializer jsonSerializer, ILogger logger, IApplicationHost appHost)
        {
            if (packageManager == null)
            {
                throw new ArgumentNullException("packageManager");
            }
            if (logger == null)
            {
                throw new ArgumentNullException("logger");
            }
            if (jsonSerializer == null)
            {
                throw new ArgumentNullException("jsonSerializer");
            }
            if (httpClient == null)
            {
                throw new ArgumentNullException("httpClient");
            }

            CurrentInstallations = new List<Tuple<InstallationInfo, CancellationTokenSource>>();
            CompletedInstallations = new ConcurrentBag<InstallationInfo>();
            JsonSerializer = jsonSerializer;
            HttpClient = httpClient;
            ApplicationHost = appHost;
            _packageManager = packageManager;
            _logger = logger;
        }

        /// <summary>
        /// Gets all available packages.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="packageType">Type of the package.</param>
        /// <param name="applicationVersion">The application version.</param>
        /// <returns>Task{List{PackageInfo}}.</returns>
        public async Task<IEnumerable<PackageInfo>> GetAvailablePackages(CancellationToken cancellationToken,
            PackageType? packageType = null,
            Version applicationVersion = null)
        {
            var packages = (await _packageManager.GetAvailablePackages(cancellationToken).ConfigureAwait(false)).ToList();

            if (packageType.HasValue)
            {
                packages = packages.Where(p => p.type == packageType.Value).ToList();
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
        /// <param name="applicationVersion">The application version.</param>
        /// <returns><c>true</c> if [is package version up to date] [the specified package version info]; otherwise, <c>false</c>.</returns>
        private bool IsPackageVersionUpToDate(PackageVersionInfo packageVersionInfo, Version applicationVersion)
        {
            if (string.IsNullOrEmpty(packageVersionInfo.requiredVersionStr))
            {
                return true;
            }

            Version requiredVersion;

            return Version.TryParse(packageVersionInfo.requiredVersionStr, out requiredVersion) && applicationVersion >= requiredVersion;
        }

        /// <summary>
        /// Gets the package.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="classification">The classification.</param>
        /// <param name="version">The version.</param>
        /// <returns>Task{PackageVersionInfo}.</returns>
        public async Task<PackageVersionInfo> GetPackage(string name, PackageVersionClass classification, Version version)
        {
            var packages = await GetAvailablePackages(CancellationToken.None).ConfigureAwait(false);

            var package = packages.FirstOrDefault(p => p.name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (package == null)
            {
                return null;
            }

            return package.versions.FirstOrDefault(v => v.version.Equals(version) && v.classification == classification);
        }

        /// <summary>
        /// Gets the latest compatible version.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="classification">The classification.</param>
        /// <returns>Task{PackageVersionInfo}.</returns>
        public async Task<PackageVersionInfo> GetLatestCompatibleVersion(string name, PackageVersionClass classification = PackageVersionClass.Release)
        {
            var packages = await GetAvailablePackages(CancellationToken.None).ConfigureAwait(false);

            return GetLatestCompatibleVersion(packages, name, classification);
        }

        /// <summary>
        /// Gets the latest compatible version.
        /// </summary>
        /// <param name="availablePackages">The available packages.</param>
        /// <param name="name">The name.</param>
        /// <param name="classification">The classification.</param>
        /// <returns>PackageVersionInfo.</returns>
        public PackageVersionInfo GetLatestCompatibleVersion(IEnumerable<PackageInfo> availablePackages, string name, PackageVersionClass classification = PackageVersionClass.Release)
        {
            var package = availablePackages.FirstOrDefault(p => p.name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (package == null)
            {
                return null;
            }

            return package.versions
                .OrderByDescending(v => v.version)
                .FirstOrDefault(v => v.classification <= classification && IsPackageVersionUpToDate(v, ApplicationHost.ApplicationVersion));
        }

        /// <summary>
        /// Gets the available plugin updates.
        /// </summary>
        /// <param name="withAutoUpdateEnabled">if set to <c>true</c> [with auto update enabled].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{IEnumerable{PackageVersionInfo}}.</returns>
        public async Task<IEnumerable<PackageVersionInfo>> GetAvailablePluginUpdates(bool withAutoUpdateEnabled, CancellationToken cancellationToken)
        {
            var catalog = await GetAvailablePackages(cancellationToken).ConfigureAwait(false);

            var plugins = ApplicationHost.Plugins;

            if (withAutoUpdateEnabled)
            {
                plugins = plugins.Where(p => p.Configuration.EnableAutoUpdate);
            }

            // Figure out what needs to be installed
            return plugins.Select(p =>
            {
                var latestPluginInfo = GetLatestCompatibleVersion(catalog, p.Name, p.Configuration.UpdateClass);

                return latestPluginInfo != null && latestPluginInfo.version > p.Version ? latestPluginInfo : null;

            }).Where(p => !CompletedInstallations.Any(i => i.Name.Equals(p.name, StringComparison.OrdinalIgnoreCase)))
            .Where(p => p != null && !string.IsNullOrWhiteSpace(p.sourceUrl));
        }

        /// <summary>
        /// Installs the package.
        /// </summary>
        /// <param name="package">The package.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">package</exception>
        public async Task InstallPackage(PackageVersionInfo package, IProgress<double> progress, CancellationToken cancellationToken)
        {
            if (package == null)
            {
                throw new ArgumentNullException("package");
            }

            if (progress == null)
            {
                throw new ArgumentNullException("progress");
            }

            if (cancellationToken == null)
            {
                throw new ArgumentNullException("cancellationToken");
            }

            var installationInfo = new InstallationInfo
            {
                Id = Guid.NewGuid(),
                Name = package.name,
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

            var innerProgress = new ActionableProgress<double> { };

            // Whenever the progress updates, update the outer progress object and InstallationInfo
            innerProgress.RegisterAction(percent =>
            {
                progress.Report(percent);

                installationInfo.PercentComplete = percent;
            });

            var linkedToken = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCancellationTokenSource.Token).Token;

            EventHelper.QueueEventIfNotNull(PackageInstalling, this, new GenericEventArgs<InstallationInfo>() { Argument = installationInfo }, _logger);

            try
            {
                await InstallPackageInternal(package, innerProgress, linkedToken).ConfigureAwait(false);

                lock (CurrentInstallations)
                {
                    CurrentInstallations.Remove(tuple);
                }

                CompletedInstallations.Add(installationInfo);

                EventHelper.QueueEventIfNotNull(PackageInstallationCompleted, this, new GenericEventArgs<InstallationInfo>() { Argument = installationInfo }, _logger);
            }
            catch (OperationCanceledException)
            {
                lock (CurrentInstallations)
                {
                    CurrentInstallations.Remove(tuple);
                }

                _logger.Info("Package installation cancelled: {0} {1}", package.name, package.versionStr);

                EventHelper.QueueEventIfNotNull(PackageInstallationCancelled, this, new GenericEventArgs<InstallationInfo>() { Argument = installationInfo }, _logger);

                throw;
            }
            catch
            {
                lock (CurrentInstallations)
                {
                    CurrentInstallations.Remove(tuple);
                }

                EventHelper.QueueEventIfNotNull(PackageInstallationFailed, this, new GenericEventArgs<InstallationInfo>() { Argument = installationInfo }, _logger);

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
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task InstallPackageInternal(PackageVersionInfo package, IProgress<double> progress, CancellationToken cancellationToken)
        {
            // Do the install
            await _packageManager.InstallPackage(progress, package, cancellationToken).ConfigureAwait(false);

            // Do plugin-specific processing
            if (!(Path.GetExtension(package.targetFilename) ?? "").Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                // Set last update time if we were installed before
                var plugin = ApplicationHost.Plugins.FirstOrDefault(p => p.Name.Equals(package.name, StringComparison.OrdinalIgnoreCase));

                if (plugin != null)
                {
                    // Synchronize the UpdateClass value
                    if (plugin.Configuration.UpdateClass != package.classification)
                    {
                        plugin.Configuration.UpdateClass = package.classification;
                        plugin.SaveConfiguration();
                    }

                    OnPluginUpdated(plugin, package);
                }
                else
                {
                    OnPluginInstalled(package);
                }
                
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
            ApplicationHost.RemovePlugin(plugin);

            File.Delete(plugin.AssemblyFilePath);

            OnPluginUninstalled(plugin);

            ApplicationHost.NotifyPendingRestart();
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

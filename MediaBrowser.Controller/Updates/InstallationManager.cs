using MediaBrowser.Common.Events;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Progress;
using MediaBrowser.Model.IO;
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

namespace MediaBrowser.Controller.Updates
{
    /// <summary>
    /// Manages all install, uninstall and update operations (both plugins and system)
    /// </summary>
    public class InstallationManager : BaseManager<Kernel>
    {
        /// <summary>
        /// The current installations
        /// </summary>
        public readonly List<Tuple<InstallationInfo, CancellationTokenSource>> CurrentInstallations =
            new List<Tuple<InstallationInfo, CancellationTokenSource>>();

        /// <summary>
        /// The completed installations
        /// </summary>
        public readonly ConcurrentBag<InstallationInfo> CompletedInstallations = new ConcurrentBag<InstallationInfo>();

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

            // Notify connected ui's
            Kernel.TcpManager.SendWebSocketMessage("PluginUninstalled", plugin.GetPluginInfo());
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
        public void OnPluginUpdated(IPlugin plugin, PackageVersionInfo newVersion)
        {
            _logger.Info("Plugin updated: {0} {1} {2}", newVersion.name, newVersion.version, newVersion.classification);

            EventHelper.QueueEventIfNotNull(PluginUpdated, this, new GenericEventArgs<Tuple<IPlugin, PackageVersionInfo>> { Argument = new Tuple<IPlugin, PackageVersionInfo>(plugin, newVersion) }, _logger);

            Kernel.NotifyPendingRestart();
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
        public void OnPluginInstalled(PackageVersionInfo package)
        {
            _logger.Info("New plugin installed: {0} {1} {2}", package.name, package.version, package.classification);

            EventHelper.QueueEventIfNotNull(PluginInstalled, this, new GenericEventArgs<PackageVersionInfo> { Argument = package }, _logger);

            Kernel.NotifyPendingRestart();
        }
        #endregion

        /// <summary>
        /// Gets or sets the zip client.
        /// </summary>
        /// <value>The zip client.</value>
        private IZipClient ZipClient { get; set; }

        /// <summary>
        /// The _logger
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// The _network manager
        /// </summary>
        private readonly INetworkManager _networkManager;

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
        /// <param name="kernel">The kernel.</param>
        /// <param name="httpClient">The HTTP client.</param>
        /// <param name="zipClient">The zip client.</param>
        /// <param name="networkManager">The network manager.</param>
        /// <param name="jsonSerializer">The json serializer.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="appHost">The app host.</param>
        /// <exception cref="System.ArgumentNullException">zipClient</exception>
        public InstallationManager(Kernel kernel, IHttpClient httpClient, IZipClient zipClient, INetworkManager networkManager, IJsonSerializer jsonSerializer, ILogger logger, IApplicationHost appHost)
            : base(kernel)
        {
            if (zipClient == null)
            {
                throw new ArgumentNullException("zipClient");
            }
            if (networkManager == null)
            {
                throw new ArgumentNullException("networkManager");
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

            JsonSerializer = jsonSerializer;
            HttpClient = httpClient;
            ApplicationHost = appHost;
            _networkManager = networkManager;
            _logger = logger;
            ZipClient = zipClient;
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
            var data = new Dictionary<string, string> { { "key", Kernel.PluginSecurityManager.SupporterKey }, { "mac", _networkManager.GetMacAddress() } };

            using (var json = await HttpClient.Post(Controller.Kernel.MBAdminUrl + "service/package/retrieveall", data, Kernel.ResourcePools.Mb, cancellationToken).ConfigureAwait(false))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var packages = JsonSerializer.DeserializeFromStream<List<PackageInfo>>(json).ToList();

                foreach (var package in packages)
                {
                    package.versions = package.versions.Where(v => !string.IsNullOrWhiteSpace(v.sourceUrl))
                        .OrderByDescending(v => v.version).ToList();
                }

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

            var plugins = Kernel.Plugins;

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

            Kernel.TcpManager.SendWebSocketMessage("PackageInstalling", installationInfo);

            try
            {
                await InstallPackageInternal(package, innerProgress, linkedToken).ConfigureAwait(false);

                lock (CurrentInstallations)
                {
                    CurrentInstallations.Remove(tuple);
                }

                CompletedInstallations.Add(installationInfo);

                Kernel.TcpManager.SendWebSocketMessage("PackageInstallationCompleted", installationInfo);
            }
            catch (OperationCanceledException)
            {
                lock (CurrentInstallations)
                {
                    CurrentInstallations.Remove(tuple);
                }

                _logger.Info("Package installation cancelled: {0} {1}", package.name, package.versionStr);

                Kernel.TcpManager.SendWebSocketMessage("PackageInstallationCancelled", installationInfo);

                throw;
            }
            catch
            {
                lock (CurrentInstallations)
                {
                    CurrentInstallations.Remove(tuple);
                }

                Kernel.TcpManager.SendWebSocketMessage("PackageInstallationFailed", installationInfo);

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
            // Target based on if it is an archive or single assembly
            //  zip archives are assumed to contain directory structures relative to our ProgramDataPath
            var isArchive = string.Equals(Path.GetExtension(package.sourceUrl), ".zip", StringComparison.OrdinalIgnoreCase);
            var target = isArchive ? Kernel.ApplicationPaths.ProgramDataPath : Path.Combine(Kernel.ApplicationPaths.PluginsPath, package.targetFilename);

            // Download to temporary file so that, if interrupted, it won't destroy the existing installation
            var tempFile = await HttpClient.GetTempFile(package.sourceUrl, Kernel.ResourcePools.Mb, cancellationToken, progress).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            // Validate with a checksum
            if (package.checksum != Guid.Empty) // support for legacy uploads for now
            {
                using (var crypto = new MD5CryptoServiceProvider())
                using (var stream = new BufferedStream(File.OpenRead(tempFile), 100000))
                {
                    var check = Guid.Parse(BitConverter.ToString(crypto.ComputeHash(stream)).Replace("-", String.Empty));
                    if (check != package.checksum)
                    {
                        throw new ApplicationException(string.Format("Download validation failed for {0}.  Probably corrupted during transfer.", package.name));
                    }
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Success - move it to the real target based on type
            if (isArchive)
            {
                try
                {
                    ZipClient.ExtractAll(tempFile, target, true);
                }
                catch (IOException e)
                {
                    _logger.ErrorException("Error attempting to extract archive from {0} to {1}", e, tempFile, target);
                    throw;
                }

            }
            else
            {
                try
                {
                    File.Copy(tempFile, target, true);
                    File.Delete(tempFile);
                }
                catch (IOException e)
                {
                    _logger.ErrorException("Error attempting to move file from {0} to {1}", e, tempFile, target);
                    throw;
                }
            }

            // Set last update time if we were installed before
            var plugin = Kernel.Plugins.FirstOrDefault(p => p.Name.Equals(package.name, StringComparison.OrdinalIgnoreCase));

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

        /// <summary>
        /// Uninstalls a plugin
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        /// <exception cref="System.ArgumentException"></exception>
        public void UninstallPlugin(IPlugin plugin)
        {
            if (plugin.IsCorePlugin)
            {
                throw new ArgumentException(string.Format("{0} cannot be uninstalled because it is a core plugin.", plugin.Name));
            }

            plugin.OnUninstalling();

            // Remove it the quick way for now
            Kernel.RemovePlugin(plugin);

            File.Delete(plugin.AssemblyFilePath);

            OnPluginUninstalled(plugin);

            Kernel.NotifyPendingRestart();
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool dispose)
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
            base.Dispose(dispose);
        }
    }
}

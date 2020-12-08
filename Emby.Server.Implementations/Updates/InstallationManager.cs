#pragma warning disable CS1591

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Events;
using MediaBrowser.Common;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Json;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Updates;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Events.Updates;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Updates;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Updates
{
    /// <summary>
    /// Manages all install, uninstall, and update operations for the system and individual plugins.
    /// </summary>
    public class InstallationManager : IInstallationManager
    {
        /// <summary>
        /// The logger.
        /// </summary>
        private readonly ILogger<InstallationManager> _logger;
        private readonly IApplicationPaths _appPaths;
        private readonly IEventManager _eventManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServerConfigurationManager _config;
        private readonly IFileSystem _fileSystem;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        /// <summary>
        /// Gets the application host.
        /// </summary>
        /// <value>The application host.</value>
        private readonly IServerApplicationHost _applicationHost;

        private readonly IZipClient _zipClient;

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
            IServerApplicationHost appHost,
            IApplicationPaths appPaths,
            IEventManager eventManager,
            IHttpClientFactory httpClientFactory,
            IServerConfigurationManager config,
            IFileSystem fileSystem,
            IZipClient zipClient)
        {
            _currentInstallations = new List<(InstallationInfo, CancellationTokenSource)>();
            _completedInstallationsInternal = new ConcurrentBag<InstallationInfo>();

            _logger = logger;
            _applicationHost = appHost;
            _appPaths = appPaths;
            _eventManager = eventManager;
            _httpClientFactory = httpClientFactory;
            _config = config;
            _fileSystem = fileSystem;
            _zipClient = zipClient;
            _jsonSerializerOptions = JsonDefaults.GetOptions();
        }

        /// <inheritdoc />
        public IEnumerable<InstallationInfo> CompletedInstallations => _completedInstallationsInternal;

        /// <inheritdoc />
        public async Task<IList<PackageInfo>> GetPackages(string manifestName, string manifest, CancellationToken cancellationToken = default)
        {
            try
            {
                var packages = await _httpClientFactory.CreateClient(NamedClient.Default)
                    .GetFromJsonAsync<List<PackageInfo>>(new Uri(manifest), _jsonSerializerOptions, cancellationToken).ConfigureAwait(false);
                if (packages == null)
                {
                    return Array.Empty<PackageInfo>();
                }

                // Store the repository and repository url with each version, as they may be spread apart.
                foreach (var entry in packages)
                {
                    foreach (var ver in entry.versions)
                    {
                        ver.repositoryName = manifestName;
                        ver.repositoryUrl = manifest;
                    }
                }

                return packages;
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize the plugin manifest retrieved from {Manifest}", manifest);
                return Array.Empty<PackageInfo>();
            }
            catch (UriFormatException ex)
            {
                _logger.LogError(ex, "The URL configured for the plugin repository manifest URL is not valid: {Manifest}", manifest);
                return Array.Empty<PackageInfo>();
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "An error occurred while accessing the plugin manifest: {Manifest}", manifest);
                return Array.Empty<PackageInfo>();
            }
        }

        private static void MergeSort(IList<VersionInfo> source, IList<VersionInfo> dest)
        {
            int sLength = source.Count - 1;
            int dLength = dest.Count;
            int s = 0, d = 0;
            var sourceVersion = source[0].VersionNumber;
            var destVersion = dest[0].VersionNumber;

            while (d < dLength)
            {
                if (sourceVersion.CompareTo(destVersion) >= 0)
                {
                    if (s < sLength)
                    {
                        sourceVersion = source[++s].VersionNumber;
                    }
                    else
                    {
                        // Append all of destination to the end of source.
                        while (d < dLength)
                        {
                            source.Add(dest[d++]);
                        }

                        break;
                    }
                }
                else
                {
                    source.Insert(s++, dest[d++]);
                    if (d >= dLength)
                    {
                        break;
                    }

                    sLength++;
                    destVersion = dest[d].VersionNumber;
                }
            }
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<PackageInfo>> GetAvailablePackages(CancellationToken cancellationToken = default)
        {
            var result = new List<PackageInfo>();
            foreach (RepositoryInfo repository in _config.Configuration.PluginRepositories)
            {
                if (repository.Enabled)
                {
                    // Where repositories have the same content, the details of the first is taken.
                    foreach (var package in await GetPackages(repository.Name, repository.Url, cancellationToken).ConfigureAwait(true))
                    {
                        if (!Guid.TryParse(package.guid, out var packageGuid))
                        {
                            // Package doesn't have a valid GUID, skip.
                            continue;
                        }

                        var existing = FilterPackages(result, package.name, packageGuid).FirstOrDefault();
                        if (existing != null)
                        {
                            // Assumption is both lists are ordered, so slot these into the correct place.
                            MergeSort(existing.versions, package.versions);
                        }
                        else
                        {
                            result.Add(package);
                        }
                    }
                }
            }

            return result;
        }

        /// <inheritdoc />
        public IEnumerable<PackageInfo> FilterPackages(
            IEnumerable<PackageInfo> availablePackages,
            string name = null,
            Guid guid = default,
            Version specificVersion = null)
        {
            if (name != null)
            {
                availablePackages = availablePackages.Where(x => x.name.Equals(name, StringComparison.OrdinalIgnoreCase));
            }

            if (guid != Guid.Empty)
            {
                availablePackages = availablePackages.Where(x => Guid.Parse(x.guid) == guid);
            }

            if (specificVersion != null)
            {
                availablePackages = availablePackages.Where(x => x.versions.Where(y => y.VersionNumber.Equals(specificVersion)).Any());
            }

            return availablePackages;
        }

        /// <inheritdoc />
        public IEnumerable<InstallationInfo> GetCompatibleVersions(
            IEnumerable<PackageInfo> availablePackages,
            string name = null,
            Guid guid = default,
            Version minVersion = null,
            Version specificVersion = null)
        {
            var package = FilterPackages(availablePackages, name, guid, specificVersion).FirstOrDefault();

            // Package not found in repository
            if (package == null)
            {
                yield break;
            }

            var appVer = _applicationHost.ApplicationVersion;
            var availableVersions = package.versions
                .Where(x => Version.Parse(x.targetAbi) <= appVer);

            if (specificVersion != null)
            {
                availableVersions = availableVersions.Where(x => x.VersionNumber.Equals(specificVersion));
            }
            else if (minVersion != null)
            {
                availableVersions = availableVersions.Where(x => x.VersionNumber >= minVersion);
            }

            foreach (var v in availableVersions.OrderByDescending(x => x.VersionNumber))
            {
                yield return new InstallationInfo
                {
                    Changelog = v.changelog,
                    Guid = new Guid(package.guid),
                    Name = package.name,
                    Version = v.VersionNumber,
                    SourceUrl = v.sourceUrl,
                    Checksum = v.checksum
                };
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<InstallationInfo>> GetAvailablePluginUpdates(CancellationToken cancellationToken = default)
        {
            var catalog = await GetAvailablePackages(cancellationToken).ConfigureAwait(false);
            return GetAvailablePluginUpdates(catalog);
        }

        private IEnumerable<InstallationInfo> GetAvailablePluginUpdates(IReadOnlyList<PackageInfo> pluginCatalog)
        {
            var plugins = _applicationHost.GetLocalPlugins(_appPaths.PluginsPath);
            foreach (var plugin in plugins)
            {
                var compatibleVersions = GetCompatibleVersions(pluginCatalog, plugin.Name, plugin.Id, minVersion: plugin.Version);
                var version = compatibleVersions.FirstOrDefault(y => y.Version > plugin.Version);
                if (version != null && CompletedInstallations.All(x => x.Guid != version.Guid))
                {
                    yield return version;
                }
            }
        }

        /// <inheritdoc />
        public async Task InstallPackage(InstallationInfo package, CancellationToken cancellationToken)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            var innerCancellationTokenSource = new CancellationTokenSource();

            var tuple = (package, innerCancellationTokenSource);

            // Add it to the in-progress list
            lock (_currentInstallationsLock)
            {
                _currentInstallations.Add(tuple);
            }

            using var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, innerCancellationTokenSource.Token);
            var linkedToken = linkedTokenSource.Token;

            await _eventManager.PublishAsync(new PluginInstallingEventArgs(package)).ConfigureAwait(false);

            try
            {
                var isUpdate = await InstallPackageInternal(package, linkedToken).ConfigureAwait(false);

                lock (_currentInstallationsLock)
                {
                    _currentInstallations.Remove(tuple);
                }

                _completedInstallationsInternal.Add(package);
                await _eventManager.PublishAsync(isUpdate
                    ? (GenericEventArgs<InstallationInfo>)new PluginUpdatedEventArgs(package)
                    : new PluginInstalledEventArgs(package)).ConfigureAwait(false);

                _applicationHost.NotifyPendingRestart();
            }
            catch (OperationCanceledException)
            {
                lock (_currentInstallationsLock)
                {
                    _currentInstallations.Remove(tuple);
                }

                _logger.LogInformation("Package installation cancelled: {0} {1}", package.Name, package.Version);

                await _eventManager.PublishAsync(new PluginInstallationCancelledEventArgs(package)).ConfigureAwait(false);

                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Package installation failed");

                lock (_currentInstallationsLock)
                {
                    _currentInstallations.Remove(tuple);
                }

                await _eventManager.PublishAsync(new InstallationFailedEventArgs
                {
                    InstallationInfo = package,
                    Exception = ex
                }).ConfigureAwait(false);

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
        private async Task<bool> InstallPackageInternal(InstallationInfo package, CancellationToken cancellationToken)
        {
            // Set last update time if we were installed before
            IPlugin plugin = _applicationHost.Plugins.FirstOrDefault(p => p.Id == package.Guid)
                           ?? _applicationHost.Plugins.FirstOrDefault(p => p.Name.Equals(package.Name, StringComparison.OrdinalIgnoreCase));

            // Do the install
            await PerformPackageInstallation(package, cancellationToken).ConfigureAwait(false);

            // Do plugin-specific processing
            _logger.LogInformation(plugin == null ? "New plugin installed: {0} {1}" : "Plugin updated: {0} {1}", package.Name, package.Version);

            return plugin != null;
        }

        private async Task PerformPackageInstallation(InstallationInfo package, CancellationToken cancellationToken)
        {
            var extension = Path.GetExtension(package.SourceUrl);
            if (!string.Equals(extension, ".zip", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError("Only zip packages are supported. {SourceUrl} is not a zip archive.", package.SourceUrl);
                return;
            }

            // Always override the passed-in target (which is a file) and figure it out again
            string targetDir = Path.Combine(_appPaths.PluginsPath, package.Name);

            using var response = await _httpClientFactory.CreateClient(NamedClient.Default)
                .GetAsync(new Uri(package.SourceUrl), cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            // CA5351: Do Not Use Broken Cryptographic Algorithms
#pragma warning disable CA5351
            using var md5 = MD5.Create();
            cancellationToken.ThrowIfCancellationRequested();

            var hash = Convert.ToHexString(md5.ComputeHash(stream));
            if (!string.Equals(package.Checksum, hash, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogError(
                    "The checksums didn't match while installing {Package}, expected: {Expected}, got: {Received}",
                    package.Name,
                    package.Checksum,
                    hash);
                throw new InvalidDataException("The checksum of the received data doesn't match.");
            }

            // Version folder as they cannot be overwritten in Windows.
            targetDir += "_" + package.Version;

            if (Directory.Exists(targetDir))
            {
                try
                {
                    Directory.Delete(targetDir, true);
                }
                catch
                {
                    // Ignore any exceptions.
                }
            }

            stream.Position = 0;
            _zipClient.ExtractAllFromZip(stream, targetDir, true);

#pragma warning restore CA5351
        }

        /// <summary>
        /// Uninstalls a plugin.
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        public void UninstallPlugin(IPlugin plugin)
        {
            if (!plugin.CanUninstall)
            {
                _logger.LogWarning("Attempt to delete non removable plugin {0}, ignoring request", plugin.Name);
                return;
            }

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

            try
            {
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
            }
            catch
            {
                // Ignore file errors.
            }

            var list = _config.Configuration.UninstalledPlugins.ToList();
            var filename = Path.GetFileName(path);
            if (!list.Contains(filename, StringComparer.OrdinalIgnoreCase))
            {
                list.Add(filename);
                _config.Configuration.UninstalledPlugins = list.ToArray();
                _config.SaveConfiguration();
            }

            _eventManager.Publish(new PluginUninstalledEventArgs(plugin));

            _applicationHost.NotifyPendingRestart();
        }

        /// <inheritdoc/>
        public bool CancelInstallation(Guid id)
        {
            lock (_currentInstallationsLock)
            {
                var install = _currentInstallations.Find(x => x.info.Guid == id);
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
        /// Releases unmanaged and optionally managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources or <c>false</c> to release only unmanaged resources.</param>
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

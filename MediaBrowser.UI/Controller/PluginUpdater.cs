using MediaBrowser.Common.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.UI.Controller
{
    /// <summary>
    /// This keeps ui plugin assemblies in sync with plugins installed on the server
    /// </summary>
    public class PluginUpdater
    {
        private readonly ILogger _logger;

        public PluginUpdater(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Updates the plugins.
        /// </summary>
        /// <returns>Task{PluginUpdateResult}.</returns>
        public async Task<PluginUpdateResult> UpdatePlugins()
        {
            _logger.Info("Downloading list of installed plugins");
            var allInstalledPlugins = await UIKernel.Instance.ApiClient.GetInstalledPluginsAsync().ConfigureAwait(false);

            var uiPlugins = allInstalledPlugins.Where(p => p.DownloadToUI).ToList();

            var result = new PluginUpdateResult { };

            result.DeletedPlugins = DeleteUninstalledPlugins(uiPlugins);

            await DownloadPluginAssemblies(uiPlugins, result).ConfigureAwait(false);

            result.UpdatedConfigurations = await DownloadPluginConfigurations(uiPlugins).ConfigureAwait(false);

            return result;
        }

        /// <summary>
        /// Downloads plugin assemblies from the server, if they need to be installed or updated.
        /// </summary>
        /// <param name="uiPlugins">The UI plugins.</param>
        /// <param name="result">The result.</param>
        /// <returns>Task.</returns>
        private async Task DownloadPluginAssemblies(IEnumerable<PluginInfo> uiPlugins, PluginUpdateResult result)
        {
            var newlyInstalledPlugins = new List<PluginInfo>();
            var updatedPlugins = new List<PluginInfo>();

            // Loop through the list of plugins that are on the server
            foreach (var pluginInfo in uiPlugins)
            {
                // See if it is already installed in the UI
                var currentAssemblyPath = Path.Combine(UIKernel.Instance.ApplicationPaths.PluginsPath, pluginInfo.AssemblyFileName);

                var isPluginInstalled = File.Exists(currentAssemblyPath);

                // Download the plugin if it is not present, or if the current version is out of date
                bool downloadPlugin;

                if (!isPluginInstalled)
                {
                    downloadPlugin = true;
                    _logger.Info("{0} is not installed and needs to be downloaded.", pluginInfo.Name);
                }
                else
                {
                    var serverVersion = Version.Parse(pluginInfo.Version);

                    var fileVersion = FileVersionInfo.GetVersionInfo(currentAssemblyPath).FileVersion ?? string.Empty;

                    downloadPlugin = string.IsNullOrEmpty(fileVersion) || Version.Parse(fileVersion) < serverVersion;

                    if (downloadPlugin)
                    {
                        _logger.Info("{0} has an updated version on the server and needs to be downloaded. Server version: {1}, UI version: {2}", pluginInfo.Name, serverVersion, fileVersion);
                    }
                }

                if (downloadPlugin)
                {
                    if (UIKernel.Instance.ApplicationVersion < Version.Parse(pluginInfo.MinimumRequiredUIVersion))
                    {
                        _logger.Warn("Can't download new version of {0} because the application needs to be updated first.", pluginInfo.Name);
                        continue;
                    }

                    try
                    {
                        await DownloadPlugin(pluginInfo).ConfigureAwait(false);

                        if (isPluginInstalled)
                        {
                            updatedPlugins.Add(pluginInfo);
                        }
                        else
                        {
                            newlyInstalledPlugins.Add(pluginInfo);
                        }
                    }
                    catch (HttpException ex)
                    {
                        _logger.ErrorException("Error downloading {0} configuration", ex, pluginInfo.Name);
                    }
                    catch (IOException ex)
                    {
                        _logger.ErrorException("Error saving plugin assembly for {0}", ex, pluginInfo.Name);
                    }
                }
            }

            result.NewlyInstalledPlugins = newlyInstalledPlugins;
            result.UpdatedPlugins = updatedPlugins;
        }

        /// <summary>
        /// Downloads plugin configurations from the server.
        /// </summary>
        /// <param name="uiPlugins">The UI plugins.</param>
        /// <returns>Task{List{PluginInfo}}.</returns>
        private async Task<List<PluginInfo>> DownloadPluginConfigurations(IEnumerable<PluginInfo> uiPlugins)
        {
            var updatedPlugins = new List<PluginInfo>();

            // Loop through the list of plugins that are on the server
            foreach (var pluginInfo in uiPlugins
                .Where(p => UIKernel.Instance.ApplicationVersion >= Version.Parse(p.MinimumRequiredUIVersion)))
            {
                // See if it is already installed in the UI
                var path = Path.Combine(UIKernel.Instance.ApplicationPaths.PluginConfigurationsPath, pluginInfo.ConfigurationFileName);

                var download = false;

                if (!File.Exists(path))
                {
                    download = true;
                    _logger.Info("{0} configuration was not found needs to be downloaded.", pluginInfo.Name);
                }
                else if (File.GetLastWriteTimeUtc(path) < pluginInfo.ConfigurationDateLastModified)
                {
                    download = true;
                    _logger.Info("{0} has an updated configuration on the server and needs to be downloaded.", pluginInfo.Name);
                }

                if (download)
                {
                    if (UIKernel.Instance.ApplicationVersion < Version.Parse(pluginInfo.MinimumRequiredUIVersion))
                    {
                        _logger.Warn("Can't download updated configuration of {0} because the application needs to be updated first.", pluginInfo.Name);
                        continue;
                    }
                    
                    try
                    {
                        await DownloadPluginConfiguration(pluginInfo, path).ConfigureAwait(false);

                        updatedPlugins.Add(pluginInfo);
                    }
                    catch (HttpException ex)
                    {
                        _logger.ErrorException("Error downloading {0} configuration", ex, pluginInfo.Name);
                    }
                    catch (IOException ex)
                    {
                        _logger.ErrorException("Error saving plugin configuration to {0}", ex, path);
                    }
                }
            }

            return updatedPlugins;
        }

        /// <summary>
        /// Downloads a plugin assembly from the server
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        /// <returns>Task.</returns>
        private async Task DownloadPlugin(PluginInfo plugin)
        {
            _logger.Info("Downloading {0} Plugin", plugin.Name);

            var path = Path.Combine(UIKernel.Instance.ApplicationPaths.PluginsPath, plugin.AssemblyFileName);

            // First download to a MemoryStream. This way if the download is cut off, we won't be left with a partial file
            using (var memoryStream = new MemoryStream())
            {
                var assemblyStream = await UIKernel.Instance.ApiClient.GetPluginAssemblyAsync(plugin).ConfigureAwait(false);

                await assemblyStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                memoryStream.Position = 0;

                using (var fileStream = new FileStream(path, FileMode.Create))
                {
                    await memoryStream.CopyToAsync(fileStream).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Downloads the latest configuration for a plugin
        /// </summary>
        /// <param name="pluginInfo">The plugin info.</param>
        /// <param name="path">The path.</param>
        /// <returns>Task.</returns>
        private async Task DownloadPluginConfiguration(PluginInfo pluginInfo, string path)
        {
            _logger.Info("Downloading {0} Configuration", pluginInfo.Name);

            // First download to a MemoryStream. This way if the download is cut off, we won't be left with a partial file
            using (var stream = await UIKernel.Instance.ApiClient.GetPluginConfigurationFileAsync(pluginInfo.Id).ConfigureAwait(false))
            {
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream).ConfigureAwait(false);

                    memoryStream.Position = 0;

                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, true))
                    {
                        await memoryStream.CopyToAsync(fs).ConfigureAwait(false);
                    }
                }
            }

            File.SetLastWriteTimeUtc(path, pluginInfo.ConfigurationDateLastModified);
        }

        /// <summary>
        /// Deletes any plugins that have been uninstalled from the server
        /// </summary>
        /// <param name="uiPlugins">The UI plugins.</param>
        /// <returns>IEnumerable{System.String}.</returns>
        private IEnumerable<string> DeleteUninstalledPlugins(IEnumerable<PluginInfo> uiPlugins)
        {
            var deletedPlugins = new List<string>();

            foreach (var plugin in Directory.EnumerateFiles(UIKernel.Instance.ApplicationPaths.PluginsPath, "*.dll", SearchOption.TopDirectoryOnly)
                .Select(Path.GetFileName)
                .ToList())
            {
                var serverPlugin = uiPlugins.FirstOrDefault(p => p.AssemblyFileName.Equals(plugin, StringComparison.OrdinalIgnoreCase));

                if (serverPlugin == null)
                {
                    try
                    {
                        DeletePlugin(plugin);

                        deletedPlugins.Add(plugin);
                    }
                    catch (IOException ex)
                    {
                        _logger.ErrorException("Error deleting plugin assembly {0}", ex, plugin);
                    }
                }
            }

            return deletedPlugins;
        }

        /// <summary>
        /// Deletes an installed ui plugin.
        /// Leaves config and data behind in the event it is later re-installed
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        private void DeletePlugin(string plugin)
        {
            _logger.Info("Deleting {0} Plugin", plugin);

            if (File.Exists(plugin))
            {
                File.Delete(plugin);
            }
        }
    }

    /// <summary>
    /// Class PluginUpdateResult
    /// </summary>
    public class PluginUpdateResult
    {
        /// <summary>
        /// Gets or sets the deleted plugins.
        /// </summary>
        /// <value>The deleted plugins.</value>
        public IEnumerable<string> DeletedPlugins { get; set; }
        /// <summary>
        /// Gets or sets the newly installed plugins.
        /// </summary>
        /// <value>The newly installed plugins.</value>
        public IEnumerable<PluginInfo> NewlyInstalledPlugins { get; set; }
        /// <summary>
        /// Gets or sets the updated plugins.
        /// </summary>
        /// <value>The updated plugins.</value>
        public IEnumerable<PluginInfo> UpdatedPlugins { get; set; }
        /// <summary>
        /// Gets or sets the updated configurations.
        /// </summary>
        /// <value>The updated configurations.</value>
        public IEnumerable<PluginInfo> UpdatedConfigurations { get; set; }
    }
}

using MediaBrowser.Common.Logging;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Common.Serialization;
using MediaBrowser.Model.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
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
        /// <summary>
        /// Gets the list of currently installed UI plugins
        /// </summary>
        [ImportMany(typeof(BasePlugin))]
        private IEnumerable<BasePlugin> CurrentPlugins { get; set; }

        private CompositionContainer CompositionContainer { get; set; }

        public async Task<PluginUpdateResult> UpdatePlugins()
        {
            // First load the plugins that are currently installed
            ReloadComposableParts();

            Logger.LogInfo("Downloading list of installed plugins");
            PluginInfo[] allInstalledPlugins = await UIKernel.Instance.ApiClient.GetInstalledPluginsAsync().ConfigureAwait(false);

            IEnumerable<PluginInfo> uiPlugins = allInstalledPlugins.Where(p => p.DownloadToUI);

            PluginUpdateResult result = new PluginUpdateResult();

            result.DeletedPlugins = DeleteUninstalledPlugins(uiPlugins);

            await DownloadPluginAssemblies(uiPlugins, result).ConfigureAwait(false);

            // If any new assemblies were downloaded we'll have to reload the CurrentPlugins list
            if (result.NewlyInstalledPlugins.Any())
            {
                ReloadComposableParts();
            }

            result.UpdatedConfigurations = await DownloadPluginConfigurations(uiPlugins).ConfigureAwait(false);

            CompositionContainer.Dispose();

            return result;
        }

        /// <summary>
        /// Downloads plugin assemblies from the server, if they need to be installed or updated.
        /// </summary>
        private async Task DownloadPluginAssemblies(IEnumerable<PluginInfo> uiPlugins, PluginUpdateResult result)
        {
            List<PluginInfo> newlyInstalledPlugins = new List<PluginInfo>();
            List<PluginInfo> updatedPlugins = new List<PluginInfo>();

            // Loop through the list of plugins that are on the server
            foreach (PluginInfo pluginInfo in uiPlugins)
            {
                // See if it is already installed in the UI
                BasePlugin installedPlugin = CurrentPlugins.FirstOrDefault(p => p.AssemblyFileName.Equals(pluginInfo.AssemblyFileName, StringComparison.OrdinalIgnoreCase));

                // Download the plugin if it is not present, or if the current version is out of date
                bool downloadPlugin = installedPlugin == null;

                if (installedPlugin != null)
                {
                    Version serverVersion = Version.Parse(pluginInfo.Version);

                    downloadPlugin = serverVersion > installedPlugin.Version;
                }

                if (downloadPlugin)
                {
                    await DownloadPlugin(pluginInfo).ConfigureAwait(false);

                    if (installedPlugin == null)
                    {
                        newlyInstalledPlugins.Add(pluginInfo);
                    }
                    else
                    {
                        updatedPlugins.Add(pluginInfo);
                    }
                }
            }

            result.NewlyInstalledPlugins = newlyInstalledPlugins;
            result.UpdatedPlugins = updatedPlugins;
        }

        /// <summary>
        /// Downloads plugin configurations from the server.
        /// </summary>
        private async Task<List<PluginInfo>> DownloadPluginConfigurations(IEnumerable<PluginInfo> uiPlugins)
        {
            List<PluginInfo> updatedPlugins = new List<PluginInfo>();

            // Loop through the list of plugins that are on the server
            foreach (PluginInfo pluginInfo in uiPlugins)
            {
                // See if it is already installed in the UI
                BasePlugin installedPlugin = CurrentPlugins.First(p => p.AssemblyFileName.Equals(pluginInfo.AssemblyFileName, StringComparison.OrdinalIgnoreCase));

                if (installedPlugin.ConfigurationDateLastModified < pluginInfo.ConfigurationDateLastModified)
                {
                    await DownloadPluginConfiguration(installedPlugin, pluginInfo).ConfigureAwait(false);

                    updatedPlugins.Add(pluginInfo);
                }
            }

            return updatedPlugins;
        }

        /// <summary>
        /// Downloads a plugin assembly from the server
        /// </summary>
        private async Task DownloadPlugin(PluginInfo plugin)
        {
            Logger.LogInfo("Downloading {0} Plugin", plugin.Name);

            string path = Path.Combine(UIKernel.Instance.ApplicationPaths.PluginsPath, plugin.AssemblyFileName);

            // First download to a MemoryStream. This way if the download is cut off, we won't be left with a partial file
            using (MemoryStream memoryStream = new MemoryStream())
            {
                Stream assemblyStream = await UIKernel.Instance.ApiClient.GetPluginAssemblyAsync(plugin).ConfigureAwait(false);

                await assemblyStream.CopyToAsync(memoryStream).ConfigureAwait(false);

                memoryStream.Position = 0;

                using (FileStream fileStream = new FileStream(path, FileMode.Create))
                {
                    await memoryStream.CopyToAsync(fileStream).ConfigureAwait(false);
                }
            }
        }

        /// <summary>
        /// Downloads the latest configuration for a plugin
        /// </summary>
        private async Task DownloadPluginConfiguration(BasePlugin plugin, PluginInfo pluginInfo)
        {
            Logger.LogInfo("Downloading {0} Configuration", plugin.Name);

            object config = await UIKernel.Instance.ApiClient.GetPluginConfigurationAsync(pluginInfo, plugin.ConfigurationType).ConfigureAwait(false);

            XmlSerializer.SerializeToFile(config, plugin.ConfigurationFilePath);

            File.SetLastWriteTimeUtc(plugin.ConfigurationFilePath, pluginInfo.ConfigurationDateLastModified);
        }

        /// <summary>
        /// Deletes any plugins that have been uninstalled from the server
        /// </summary>
        private IEnumerable<string> DeleteUninstalledPlugins(IEnumerable<PluginInfo> uiPlugins)
        {
            var deletedPlugins = new List<string>();

            foreach (BasePlugin plugin in CurrentPlugins)
            {
                PluginInfo latest = uiPlugins.FirstOrDefault(p => p.AssemblyFileName.Equals(plugin.AssemblyFileName, StringComparison.OrdinalIgnoreCase));

                if (latest == null)
                {
                    DeletePlugin(plugin);

                    deletedPlugins.Add(plugin.Name);
                }
            }

            return deletedPlugins;
        }

        /// <summary>
        /// Deletes an installed ui plugin.
        /// Leaves config and data behind in the event it is later re-installed
        /// </summary>
        private void DeletePlugin(BasePlugin plugin)
        {
            Logger.LogInfo("Deleting {0} Plugin", plugin.Name);

            string path = plugin.AssemblyFilePath;

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Re-uses MEF within the kernel to discover installed plugins
        /// </summary>
        private void ReloadComposableParts()
        {
            if (CompositionContainer != null)
            {
                CompositionContainer.Dispose();
            }

            CompositionContainer = UIKernel.Instance.GetCompositionContainer();

            CompositionContainer.ComposeParts(this);

            CompositionContainer.Catalog.Dispose();

            foreach (BasePlugin plugin in CurrentPlugins)
            {
                plugin.Initialize(UIKernel.Instance, false);
            }
        }
    }

    public class PluginUpdateResult
    {
        public IEnumerable<string> DeletedPlugins { get; set; }
        public IEnumerable<PluginInfo> NewlyInstalledPlugins { get; set; }
        public IEnumerable<PluginInfo> UpdatedPlugins { get; set; }
        public IEnumerable<PluginInfo> UpdatedConfigurations { get; set; }
    }
}

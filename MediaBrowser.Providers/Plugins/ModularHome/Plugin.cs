using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Plugins.ModularHome.Configuration;
using Newtonsoft.Json.Linq;

namespace MediaBrowser.Providers.Plugins.ModularHome
{
    /// <summary>
    /// Modular Home Plugin.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IPlugin, IHasWebPages
    {
        /// <summary>
        /// Static Instance of this.
        /// </summary>
        public static Plugin? Instance { get; private set; }

        /// <inheritdoc/>
        public override Guid Id => Guid.Parse("eb1cd147-f300-4860-a087-c85a07d6ab94");

        /// <inheritdoc/>
        public override string Name => "ModularHome";

        /// <inheritdoc/>
        public override string Description => "Enables functionality to have a more dynamic home screen, sections can be added by other plugins and each user can configure what does and doesn't appear on their homescreen.";

        /// <inheritdoc/>
        public override string ConfigurationFileName => "Jellyfin.Plugin.ModularHome.xml";

        private readonly IPluginPagesManager _pluginPagesManager;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="applicationPaths">Instance of <see cref="IApplicationPaths"/> interface.</param>
        /// <param name="xmlSerializer">Instance of <see cref="IXmlSerializer"/> interface.</param>
        /// <param name="pluginPagesManager">Instance of <see cref="IPluginPagesManager"/> interface.</param>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, IPluginPagesManager pluginPagesManager)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;

            _pluginPagesManager = pluginPagesManager;

            _pluginPagesManager.RegisterPluginPage(new PluginPage
            {
                Id = "ModularHome",
                DisplayText = "Modular Home",
                Url = "/ModularHomeViews/settings",
                Icon = "ballot"
            });
        }

        /// <inheritdoc/>
        public IEnumerable<PluginPageInfo> GetPages()
        {
            return Array.Empty<PluginPageInfo>();
        }

        /// <summary>
        /// Get the views that the plugin serves.
        /// </summary>
        /// <returns>Array of <see cref="PluginPageInfo"/>.</returns>
        public IEnumerable<PluginPageInfo> GetViews()
        {
            return new[]
            {
                new PluginPageInfo
                {
                    Name = "settings",
                    EmbeddedResourcePath = $"{GetType().Namespace}.Config.settings.html"
                },
                new PluginPageInfo
                {
                    Name = "settings.js",
                    EmbeddedResourcePath = $"{GetType().Namespace}.Config.settings.js"
                },
            };
        }
    }
}

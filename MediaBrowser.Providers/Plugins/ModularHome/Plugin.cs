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
    public class Plugin : BasePlugin<PluginConfiguration>, IPlugin, IHasWebPages
    {
        public static Plugin Instance { get; private set; }

        public override Guid Id => Guid.Parse("eb1cd147-f300-4860-a087-c85a07d6ab94");

        public override string Name => "ModularHome";

        public override string Description => "Enables functionality to have a more dynamic home screen, sections can be added by other plugins and each user can configure what does and doesn't appear on their homescreen.";

        public override string ConfigurationFileName => "Jellyfin.Plugin.ModularHome.xml";

        private const string ConfigFile = "config.json";

        private readonly IPluginPagesManager _pluginPagesManager;

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

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return Array.Empty<PluginPageInfo>();
        }

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

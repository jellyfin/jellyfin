#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Providers.Plugins.StudioImages
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        public static Plugin Instance { get; private set; }

        public override Guid Id => new Guid("872a7849-1171-458d-a6fb-3de3d442ad30");

        public override string Name => "Studio Images";

        public override string Description => "Get artwork for studios from any Jellyfin-compatible repository.";

        // TODO change this for a Jellyfin-hosted repository.
        public const string DefaultServer = "https://raw.github.com/MediaBrowser/MediaBrowser.Resources/master/images/imagesbyname";

        // TODO remove when plugin removed from server.
        public override string ConfigurationFileName => "Jellyfin.Plugin.StudioImages.xml";

        public IEnumerable<PluginPageInfo> GetPages()
        {
            yield return new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = GetType().Namespace + ".Configuration.config.html"
            };
        }
    }
}

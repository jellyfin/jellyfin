#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Providers.Plugins.Omdb
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public static Plugin Instance { get; private set; }

        public override Guid Id => new Guid("a628c0da-fac5-4c7e-9d1a-7134223f14c8");

        public override string Name => "OMDb";

        public override string Description => "Get metadata for movies and other video content from OMDb.";

        // TODO remove when plugin removed from server.
        public override string ConfigurationFileName => "Jellyfin.Plugin.Omdb.xml";

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

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

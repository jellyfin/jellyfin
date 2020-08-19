#pragma warning disable CS1591

using System;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Providers.Plugins.TheTvdb
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public static Plugin Instance { get; private set; }

        public override Guid Id => new Guid("a677c0da-fac5-4cde-941a-7134223f14c8");

        public override string Name => "TheTVDB";

        public override string Description => "Get metadata for movies and other video content from TheTVDB.";

        // TODO remove when plugin removed from server.
        public override string ConfigurationFileName => "Jellyfin.Plugin.TheTvdb.xml";

        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }
    }
}

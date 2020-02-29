using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Providers.Plugins.MusicBrainz
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public static Plugin Instance { get; private set; }

        public override Guid Id => new Guid("8c95c4d2-e50c-4fb0-a4f3-6c06ff0f9a1a");

        public override string Name => "MusicBrainz";

        public override string Description => "Get artist and album metadata from any MusicBrainz server.";

        public const string DefaultServer = "https://musicbrainz.org";

        public const long DefaultRateLimit = 2000u;

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

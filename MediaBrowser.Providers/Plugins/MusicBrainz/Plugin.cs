#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Reflection;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MetaBrainz.MusicBrainz;

namespace MediaBrowser.Providers.Plugins.MusicBrainz
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;

            // TODO: Change this to "Jellyfin Music Brainz Plugin" once we take it out of the server repo.
            Query.DefaultUserAgent = $"Jellyfin/{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3)} ( apps@jellyfin.org )";
            Query.DelayBetweenRequests = Instance.Configuration.RateLimit;
            Query.DefaultWebSite = Instance.Configuration.Server;
        }

        public static Plugin Instance { get; private set; }

        public override Guid Id => new Guid("8c95c4d2-e50c-4fb0-a4f3-6c06ff0f9a1a");

        public override string Name => "MusicBrainz";

        public override string Description => "Get artist and album metadata from any MusicBrainz server.";

        public const string DefaultServer = "musicbrainz.org";

        public const double DefaultRateLimit = 1.0;

        // TODO remove when plugin removed from server.
        public override string ConfigurationFileName => "Jellyfin.Plugin.MusicBrainz.xml";

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

#nullable disable

using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Providers.Plugins.StudioImages.Configuration;

namespace MediaBrowser.Providers.Plugins.StudioImages
{
    /// <summary>
    /// Artwork Plugin class.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        /// <summary>
        /// Artwork repository URL.
        /// </summary>
        public const string DefaultServer = "https://raw.githubusercontent.com/jellyfin/emby-artwork/master/studios";

        /// <summary>
        /// Initializes a new instance of the <see cref="Plugin"/> class.
        /// </summary>
        /// <param name="applicationPaths">application paths.</param>
        /// <param name="xmlSerializer">xml serializer.</param>
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
            : base(applicationPaths, xmlSerializer)
        {
            Instance = this;
        }

        /// <summary>
        /// Gets the instance of Artwork plugin.
        /// </summary>
        public static Plugin Instance { get; private set; }

        /// <inheritdoc/>
        public override Guid Id => new Guid("872a7849-1171-458d-a6fb-3de3d442ad30");

        /// <inheritdoc/>
        public override string Name => "Studio Images";

        /// <inheritdoc/>
        public override string Description => "Get artwork for studios from any Jellyfin-compatible repository.";

        // TODO remove when plugin removed from server.

        /// <inheritdoc/>
        public override string ConfigurationFileName => "Jellyfin.Plugin.StudioImages.xml";

        /// <inheritdoc/>
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

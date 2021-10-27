#nullable disable

using System;
using System.Collections.Generic;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace MediaBrowser.Providers.Plugins.Tmdb
{
    /// <summary>
    /// Plugin class for the TMDb library.
    /// </summary>
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
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
        /// Gets the instance of TMDb plugin.
        /// </summary>
        public static Plugin Instance { get; private set; }

        /// <inheritdoc/>
        public override Guid Id => new Guid("b8715ed1-6c47-4528-9ad3-f72deb539cd4");

        /// <inheritdoc/>
        public override string Name => "TMDb";

        /// <inheritdoc/>
        public override string Description => "Get metadata for movies and other video content from TheMovieDb.";

        // TODO remove when plugin removed from server.

        /// <inheritdoc/>
        public override string ConfigurationFileName => "Jellyfin.Plugin.Tmdb.xml";

        /// <summary>
        /// Return the plugin configuration page.
        /// </summary>
        /// <returns>PluginPageInfo.</returns>
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

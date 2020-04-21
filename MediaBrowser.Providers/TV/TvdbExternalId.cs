using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.TheTvdb;

namespace MediaBrowser.Providers.TV
{
    /// <summary>
    /// Tvdb external id.
    /// </summary>
    public class TvdbExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "TheTVDB";

        /// <inheritdoc />
        public string Key => MetadataProviders.Tvdb.ToString();

        /// <inheritdoc />
        public string UrlFormatString => TvdbUtils.TvdbBaseUrl + "?tab=series&id={0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Series;
    }
}

#pragma warning disable CS1591

using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Plugins.TheTvdb;

namespace MediaBrowser.Providers.TV
{
    public class TvdbExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => "TheTVDB";

        /// <inheritdoc />
        public string Key => MetadataProvider.Tvdb.ToString();

        /// <inheritdoc />
        public ExternalIdMediaType? Type => null;

        /// <inheritdoc />
        public string UrlFormatString => TvdbUtils.TvdbBaseUrl + "?tab=series&id={0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Series;
    }
}

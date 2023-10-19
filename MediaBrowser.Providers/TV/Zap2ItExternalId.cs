#pragma warning disable CS1591

using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.TV
{
    public class Zap2ItExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => "Zap2It";

        /// <inheritdoc />
        public string Key => MetadataProvider.Zap2It.ToString();

        /// <inheritdoc />
        public ExternalIdMediaType? Type => null;

        /// <inheritdoc />
        public string UrlFormatString => "http://tvlistings.zap2it.com/overview.html?programSeriesId={0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Series;
    }
}

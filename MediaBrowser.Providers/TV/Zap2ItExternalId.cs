using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.TV
{
    public class Zap2ItExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "Zap2It";

        /// <inheritdoc />
        public string Key => MetadataProviders.Zap2It.ToString();

        /// <inheritdoc />
        public string UrlFormatString => "http://tvlistings.zap2it.com/overview.html?programSeriesId={0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Series;
    }
}

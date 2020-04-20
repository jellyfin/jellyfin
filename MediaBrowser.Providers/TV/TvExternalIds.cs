using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.TheTvdb;

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

    public class TvdbSeasonExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "TheTVDB";

        /// <inheritdoc />
        public string Key => MetadataProviders.Tvdb.ToString();

        /// <inheritdoc />
        public string UrlFormatString => null;

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Season;
    }

    public class TvdbEpisodeExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "TheTVDB";

        /// <inheritdoc />
        public string Key => MetadataProviders.Tvdb.ToString();

        /// <inheritdoc />
        public string UrlFormatString => TvdbUtils.TvdbBaseUrl + "?tab=episode&id={0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Episode;
    }
}

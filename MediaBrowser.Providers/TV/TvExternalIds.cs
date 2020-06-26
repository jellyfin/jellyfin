#pragma warning disable CS1591

using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using MediaBrowser.Providers.Plugins.TheTvdb;

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

    public class TvdbSeasonExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => "TheTVDB";

        /// <inheritdoc />
        public string Key => MetadataProvider.Tvdb.ToString();

        /// <inheritdoc />
        public ExternalIdMediaType? Type => ExternalIdMediaType.Season;

        /// <inheritdoc />
        public string UrlFormatString => null;

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Season;
    }

    public class TvdbEpisodeExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => "TheTVDB";

        /// <inheritdoc />
        public string Key => MetadataProvider.Tvdb.ToString();

        /// <inheritdoc />
        public ExternalIdMediaType? Type => ExternalIdMediaType.Episode;

        /// <inheritdoc />
        public string UrlFormatString => TvdbUtils.TvdbBaseUrl + "?tab=episode&id={0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Episode;
    }
}

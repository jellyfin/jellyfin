using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.TV.TheTVDB;

namespace MediaBrowser.Providers.TV
{
    public class Zap2ItExternalId : IExternalId
    {
        public string Name => "Zap2It";

        public string Key => MetadataProviders.Zap2It.ToString();

        public string UrlFormatString => "http://tvlistings.zap2it.com/overview.html?programSeriesId={0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Series;
        }
    }

    public class TvdbExternalId : IExternalId
    {
        public string Name => "TheTVDB";

        public string Key => MetadataProviders.Tvdb.ToString();

        public string UrlFormatString => TvdbUtils.TvdbBaseUrl + "?tab=series&id={0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Series;
        }
    }

    public class TvdbSeasonExternalId : IExternalId
    {
        public string Name => "TheTVDB";

        public string Key => MetadataProviders.Tvdb.ToString();

        public string UrlFormatString => null;

        public bool Supports(IHasProviderIds item)
        {
            return item is Season;
        }
    }

    public class TvdbEpisodeExternalId : IExternalId
    {
        public string Name => "TheTVDB";

        public string Key => MetadataProviders.Tvdb.ToString();

        public string UrlFormatString => TvdbUtils.TvdbBaseUrl + "?tab=episode&id={0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Episode;
        }
    }
}

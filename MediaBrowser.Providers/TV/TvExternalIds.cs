using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.TV
{
    public class Zap2ItExternalId : IExternalId
    {
        public string Name
        {
            get { return "Zap2It"; }
        }

        public string Key
        {
            get { return MetadataProviders.Zap2It.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "http://tvlistings.zap2it.com/overview.html?programSeriesId={0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Series;
        }
    }

    public class TvdbExternalId : IExternalId
    {
        public string Name
        {
            get { return "TheTVDB"; }
        }

        public string Key
        {
            get { return MetadataProviders.Tvdb.ToString(); }
        }

        public string UrlFormatString
        {
            get { return TvdbPrescanTask.TvdbBaseUrl + "?tab=series&id={0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Series;
        }
    }

    public class TvdbSeasonExternalId : IExternalId
    {
        public string Name
        {
            get { return "TheTVDB"; }
        }

        public string Key
        {
            get { return MetadataProviders.Tvdb.ToString(); }
        }

        public string UrlFormatString
        {
            get { return null; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Season;
        }
    }

    public class TvdbEpisodeExternalId : IExternalId
    {
        public string Name
        {
            get { return "TheTVDB"; }
        }

        public string Key
        {
            get { return MetadataProviders.Tvdb.ToString(); }
        }

        public string UrlFormatString
        {
            get { return TvdbPrescanTask.TvdbBaseUrl + "?tab=episode&id={0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Episode;
        }
    }
}

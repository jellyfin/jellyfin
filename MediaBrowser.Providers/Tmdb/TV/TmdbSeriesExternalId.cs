using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Tmdb.TV
{
    public class TmdbSeriesExternalId : IExternalId
    {
        public string Name => TmdbUtils.ProviderName;

        public string Key => MetadataProviders.Tmdb.ToString();

        public string UrlFormatString => TmdbUtils.BaseTmdbUrl + "tv/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Series;
        }
    }
}

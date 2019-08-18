using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Tmdb.People
{
    public class TmdbPersonExternalId : IExternalId
    {
        public string Name => TmdbUtils.ProviderName;

        public string Key => MetadataProviders.Tmdb.ToString();

        public string UrlFormatString => TmdbUtils.BaseMovieDbUrl + "person/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Person;
        }
    }
}

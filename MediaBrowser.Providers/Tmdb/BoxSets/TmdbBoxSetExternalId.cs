using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Tmdb.BoxSets
{
    public class TmdbBoxSetExternalId : IExternalId
    {
        public string Name => TmdbUtils.ProviderName;

        public string Key => MetadataProviders.TmdbCollection.ToString();

        public string UrlFormatString => TmdbUtils.BaseMovieDbUrl + "collection/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Movie || item is MusicVideo || item is Trailer;
        }
    }
}

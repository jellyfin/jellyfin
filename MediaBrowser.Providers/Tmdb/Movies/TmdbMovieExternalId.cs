using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Tmdb.Movies
{
    public class TmdbMovieExternalId : IExternalId
    {
        public string Name => TmdbUtils.ProviderName;

        public string Key => MetadataProviders.Tmdb.ToString();

        public string UrlFormatString => TmdbUtils.BaseMovieDbUrl + "movie/{0}";

        public bool Supports(IHasProviderIds item)
        {
            // Supports images for tv movies
            if (item is LiveTvProgram tvProgram && tvProgram.IsMovie)
            {
                return true;
            }

            return item is Movie || item is MusicVideo || item is Trailer;
        }
    }
}

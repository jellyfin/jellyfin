using Jellyfin.Controller.Entities;
using Jellyfin.Controller.Entities.Movies;
using Jellyfin.Controller.Entities.TV;
using Jellyfin.Controller.LiveTv;
using Jellyfin.Controller.Providers;
using Jellyfin.Model.Entities;

namespace Jellyfin.Providers.Movies
{
    public class MovieDbMovieExternalId : IExternalId
    {
        public const string BaseMovieDbUrl = "https://www.themoviedb.org/";

        public string Name => "TheMovieDb";

        public string Key => MetadataProviders.Tmdb.ToString();

        public string UrlFormatString => BaseMovieDbUrl + "movie/{0}";

        public bool Supports(IHasProviderIds item)
        {
            // Supports images for tv movies
            var tvProgram = item as LiveTvProgram;
            if (tvProgram != null && tvProgram.IsMovie)
            {
                return true;
            }

            return item is Movie || item is MusicVideo || item is Trailer;
        }
    }

    public class MovieDbSeriesExternalId : IExternalId
    {
        public string Name => "TheMovieDb";

        public string Key => MetadataProviders.Tmdb.ToString();

        public string UrlFormatString => MovieDbMovieExternalId.BaseMovieDbUrl + "tv/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Series;
        }
    }

    public class MovieDbMovieCollectionExternalId : IExternalId
    {
        public string Name => "TheMovieDb Collection";

        public string Key => MetadataProviders.TmdbCollection.ToString();

        public string UrlFormatString => MovieDbMovieExternalId.BaseMovieDbUrl + "collection/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Movie || item is MusicVideo || item is Trailer;
        }
    }

    public class MovieDbPersonExternalId : IExternalId
    {
        public string Name => "TheMovieDb";

        public string Key => MetadataProviders.Tmdb.ToString();

        public string UrlFormatString => MovieDbMovieExternalId.BaseMovieDbUrl + "person/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Person;
        }
    }

    public class MovieDbCollectionExternalId : IExternalId
    {
        public string Name => "TheMovieDb";

        public string Key => MetadataProviders.Tmdb.ToString();

        public string UrlFormatString => MovieDbMovieExternalId.BaseMovieDbUrl + "collection/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is BoxSet;
        }
    }

    public class ImdbExternalId : IExternalId
    {
        public string Name => "IMDb";

        public string Key => MetadataProviders.Imdb.ToString();

        public string UrlFormatString => "https://www.imdb.com/title/{0}";

        public bool Supports(IHasProviderIds item)
        {
            // Supports images for tv movies
            var tvProgram = item as LiveTvProgram;
            if (tvProgram != null && tvProgram.IsMovie)
            {
                return true;
            }

            return item is Movie || item is MusicVideo || item is Series || item is Episode || item is Trailer;
        }
    }


    public class ImdbPersonExternalId : IExternalId
    {
        public string Name => "IMDb";

        public string Key => MetadataProviders.Imdb.ToString();

        public string UrlFormatString => "https://www.imdb.com/name/{0}";

        public bool Supports(IHasProviderIds item)
        {
            return item is Person;
        }
    }
}

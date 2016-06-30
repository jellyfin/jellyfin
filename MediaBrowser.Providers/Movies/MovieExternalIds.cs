using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Movies
{
    public class MovieDbMovieExternalId : IExternalId
    {
        public string Name
        {
            get { return "TheMovieDb"; }
        }

        public string Key
        {
            get { return MetadataProviders.Tmdb.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "https://www.themoviedb.org/movie/{0}"; }
        }

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
        public string Name
        {
            get { return "TheMovieDb"; }
        }

        public string Key
        {
            get { return MetadataProviders.Tmdb.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "https://www.themoviedb.org/tv/{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Series;
        }
    }

    public class MovieDbMovieCollectionExternalId : IExternalId
    {
        public string Name
        {
            get { return "TheMovieDb Collection"; }
        }

        public string Key
        {
            get { return MetadataProviders.TmdbCollection.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "https://www.themoviedb.org/collection/{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Movie || item is MusicVideo || item is Trailer;
        }
    }

    public class MovieDbPersonExternalId : IExternalId
    {
        public string Name
        {
            get { return "TheMovieDb"; }
        }

        public string Key
        {
            get { return MetadataProviders.Tmdb.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "https://www.themoviedb.org/person/{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Person;
        }
    }

    public class MovieDbCollectionExternalId : IExternalId
    {
        public string Name
        {
            get { return "TheMovieDb"; }
        }

        public string Key
        {
            get { return MetadataProviders.Tmdb.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "https://www.themoviedb.org/collection/{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is BoxSet;
        }
    }

    public class ImdbExternalId : IExternalId
    {
        public string Name
        {
            get { return "IMDb"; }
        }

        public string Key
        {
            get { return MetadataProviders.Imdb.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "http://www.imdb.com/title/{0}"; }
        }

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
        public string Name
        {
            get { return "IMDb"; }
        }

        public string Key
        {
            get { return MetadataProviders.Imdb.ToString(); }
        }

        public string UrlFormatString
        {
            get { return "http://www.imdb.com/name/{0}"; }
        }

        public bool Supports(IHasProviderIds item)
        {
            return item is Person;
        }
    }
}

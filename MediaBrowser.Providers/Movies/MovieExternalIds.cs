using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Movies
{
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

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
        /// <inheritdoc />
        public string Name => "IMDb";

        /// <inheritdoc />
        public string Key => MetadataProviders.Imdb.ToString();

        /// <inheritdoc />
        public string UrlFormatString => "https://www.imdb.com/title/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item)
        {
            // Supports images for tv movies
            if (item is LiveTvProgram tvProgram && tvProgram.IsMovie)
            {
                return true;
            }

            return item is Movie || item is MusicVideo || item is Series || item is Episode || item is Trailer;
        }
    }

    public class ImdbPersonExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "IMDb";

        /// <inheritdoc />
        public string Key => MetadataProviders.Imdb.ToString();

        /// <inheritdoc />
        public string UrlFormatString => "https://www.imdb.com/name/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Person;
    }
}

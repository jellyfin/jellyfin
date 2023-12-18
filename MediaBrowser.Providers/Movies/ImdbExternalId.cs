#pragma warning disable CS1591

using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Movies
{
    public class ImdbExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => "IMDb";

        /// <inheritdoc />
        public string Key => MetadataProvider.Imdb.ToString();

        /// <inheritdoc />
        public ExternalIdMediaType? Type => null;

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
}

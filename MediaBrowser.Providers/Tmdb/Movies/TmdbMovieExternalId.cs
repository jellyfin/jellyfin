using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;

namespace MediaBrowser.Providers.Tmdb.Movies
{
    public class TmdbMovieExternalId : IExternalId
    {
        private readonly ILocalizationManager _localizationManager;

        public TmdbMovieExternalId(ILocalizationManager localizationManager)
        {
            _localizationManager = localizationManager ?? throw new System.ArgumentNullException(nameof(localizationManager));
        }

        /// <inheritdoc />
        public string Name => string.Format("{0} {1}", TmdbUtils.ProviderName, _localizationManager.GetLocalizedString("Movie"));

        /// <inheritdoc />
        public string Key => MetadataProviders.Tmdb.ToString();

        /// <inheritdoc />
        public string UrlFormatString => TmdbUtils.BaseTmdbUrl + "movie/{0}";

        /// <inheritdoc />
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

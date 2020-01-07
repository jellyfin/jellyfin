using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;

namespace MediaBrowser.Providers.Tmdb.BoxSets
{
    public class TmdbBoxSetExternalId : IExternalId
    {
        private readonly ILocalizationManager _localizationManager;

        public TmdbBoxSetExternalId(ILocalizationManager localizationManager)
        {
            _localizationManager = localizationManager ?? throw new System.ArgumentNullException(nameof(localizationManager));
        }

        /// <inheritdoc />
        public string Name => string.Format("{0} {1}", TmdbUtils.ProviderName, _localizationManager.GetLocalizedString("BoxSet"));

        /// <inheritdoc />
        public string Key => MetadataProviders.TmdbCollection.ToString();

        /// <inheritdoc />
        public string UrlFormatString => TmdbUtils.BaseTmdbUrl + "collection/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item)
        {
            return item is Movie || item is MusicVideo || item is Trailer;
        }
    }
}

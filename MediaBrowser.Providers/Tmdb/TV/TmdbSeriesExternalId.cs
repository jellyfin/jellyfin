using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;

namespace MediaBrowser.Providers.Tmdb.TV
{
    public class TmdbSeriesExternalId : IExternalId
    {
        private readonly ILocalizationManager _localizationManager;

        public TmdbSeriesExternalId(ILocalizationManager localizationManager)
        {
            _localizationManager = localizationManager ?? throw new System.ArgumentNullException(nameof(localizationManager));
        }

        /// <inheritdoc />
        public string Name => string.Format("{0} {1}", TmdbUtils.ProviderName, _localizationManager.GetLocalizedString("Series"));

        /// <inheritdoc />
        public string Key => MetadataProviders.Tmdb.ToString();

        /// <inheritdoc />
        public string UrlFormatString => TmdbUtils.BaseTmdbUrl + "tv/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item)
        {
            return item is Series;
        }
    }
}

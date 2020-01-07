using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Globalization;

namespace MediaBrowser.Providers.Tmdb.People
{
    public class TmdbPersonExternalId : IExternalId
    {
        private readonly ILocalizationManager _localizationManager;

        public TmdbPersonExternalId(ILocalizationManager localizationManager)
        {
            _localizationManager = localizationManager ?? throw new System.ArgumentNullException(nameof(localizationManager));
        }

        /// <inheritdoc />
        public string Name => string.Format("{0} {1}", TmdbUtils.ProviderName, _localizationManager.GetLocalizedString("Person"));

        /// <inheritdoc />
        public string Key => MetadataProviders.Tmdb.ToString();

        /// <inheritdoc />
        public string UrlFormatString => TmdbUtils.BaseTmdbUrl + "person/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item)
        {
            return item is Person;
        }
    }
}

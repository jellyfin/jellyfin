using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.TV
{
    public class TvdbSeasonExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => "TheTVDB";

        /// <inheritdoc />
        public string Key => MetadataProviders.Tvdb.ToString();

        /// <inheritdoc />
        public string UrlFormatString => null;

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item) => item is Season;
    }
}

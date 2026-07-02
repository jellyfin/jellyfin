using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.Tmdb.TV
{
    /// <summary>
    /// TMDb series or season group id.
    /// </summary>
    public class TmdbEpisodeGroupId : IExternalId
    {
        internal const string ProviderKey = "TmdbEpisodeGroup";

        /// <inheritdoc />
        public string ProviderName => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public string Key => ProviderKey;

        /// <inheritdoc />
        public ExternalIdMediaType? Type => ExternalIdMediaType.ReleaseGroup;

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item)
        {
            return item is Series or Season;
        }
    }
}

using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.Tmdb.BoxSets
{
    /// <summary>
    /// External id for a TMDb box set.
    /// </summary>
    public class TmdbBoxSetExternalId : IExternalId
    {
        /// <inheritdoc />
        public string ProviderName => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public string Key => MetadataProvider.Tmdb.ToString();

        /// <inheritdoc />
        public ExternalIdMediaType? Type => ExternalIdMediaType.BoxSet;

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item)
        {
            return item is BoxSet;
        }
    }
}

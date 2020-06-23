#pragma warning disable CS1591

using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Plugins.Tmdb.TV
{
    public class TmdbSeriesExternalId : IExternalId
    {
        /// <inheritdoc />
        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc />
        public string Key => MetadataProvider.Tmdb.ToString();

        /// <inheritdoc />
        public string UrlFormatString => TmdbUtils.BaseTmdbUrl + "tv/{0}";

        /// <inheritdoc />
        public bool Supports(IHasProviderIds item)
        {
            return item is Series;
        }
    }
}

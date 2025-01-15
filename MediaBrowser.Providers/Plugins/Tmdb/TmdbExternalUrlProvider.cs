using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Plugins.Tmdb
{
    /// <summary>
    /// The TMDb external URL provider.
    /// </summary>
    public class TmdbExternalUrlProvider : IExternalUrlProvider
    {
        /// <inheritdoc />
        public string Name => TmdbUtils.ProviderName;

        /// <inheritdoc/>
        public IEnumerable<string> GetExternalUrls(BaseItem item)
        {
            switch (item)
            {
                case Movie:
                    if (item.TryGetProviderId(MetadataProvider.Tmdb, out var externalId))
                    {
                        yield return TmdbUtils.BaseTmdbUrl + $"movie/{externalId}";
                    }

                    if (item.TryGetProviderId(MetadataProvider.TmdbCollection, out var collectionExternalId))
                    {
                        yield return TmdbUtils.BaseTmdbUrl + $"collection/{collectionExternalId}";
                    }

                    break;
                case MusicVideo:
                case Trailer:
                    if (item.TryGetProviderId(MetadataProvider.TmdbCollection, out collectionExternalId))
                    {
                        yield return TmdbUtils.BaseTmdbUrl + $"collection/{collectionExternalId}";
                    }

                    break;
                case LiveTvProgram tvProgram when tvProgram.IsMovie:
                    if (item.TryGetProviderId(MetadataProvider.Tmdb, out externalId))
                    {
                        yield return TmdbUtils.BaseTmdbUrl + $"movie/{externalId}";
                    }

                    break;
                case Person:
                    if (item.TryGetProviderId(MetadataProvider.Tmdb, out externalId))
                    {
                        yield return TmdbUtils.BaseTmdbUrl + $"person/{externalId}";
                    }

                    break;
                case Series:
                    if (item.TryGetProviderId(MetadataProvider.Tmdb, out externalId))
                    {
                        yield return TmdbUtils.BaseTmdbUrl + $"tv/{externalId}";
                    }

                    break;
                case Season season:
                    if (season.Series.TryGetProviderId(MetadataProvider.Tmdb, out var seriesExternalId) && season.TryGetProviderId(MetadataProvider.Tmdb, out externalId))
                    {
                        if (string.IsNullOrEmpty(season.Series.DisplayOrder))
                        {
                            yield return TmdbUtils.BaseTmdbUrl + $"tv/{seriesExternalId}/season/{season.IndexNumber}";
                        }

                        if (season.TryGetProviderId(TmdbUtils.EpisodeGroupProviderId, out var episodeGroupId))
                        {
                            yield return TmdbUtils.BaseTmdbUrl + $"tv/{seriesExternalId}/episode_group/{episodeGroupId}/group/{externalId}";
                        }
                    }

                    break;
                case Episode episode:
                    // Only default order is supported
                    if (episode.Series.TryGetProviderId(MetadataProvider.Tmdb, out seriesExternalId) && string.IsNullOrEmpty(episode.Series.DisplayOrder) && episode.TryGetProviderId(MetadataProvider.Tmdb, out externalId))
                    {
                        yield return TmdbUtils.BaseTmdbUrl + $"tv/{seriesExternalId}/season/{episode.ParentIndexNumber}/episode/{episode.IndexNumber}";
                    }

                    break;
            }
        }
    }
}

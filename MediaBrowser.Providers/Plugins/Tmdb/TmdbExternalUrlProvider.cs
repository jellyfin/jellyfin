using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.Tmdb.TV;

namespace MediaBrowser.Providers.Plugins.Tmdb;

/// <summary>
/// External URLs for TMDb.
/// </summary>
public class TmdbExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => "TMDB";

    /// <inheritdoc/>
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        switch (item)
        {
            case Series series:
                if (series.TryGetProviderId(MetadataProvider.Tmdb, out var externalId))
                {
                    yield return TmdbUtils.BaseTmdbUrl + $"tv/{externalId}";

                    if (series.TryGetProviderId(TmdbEpisodeGroupId.ProviderKey, out var seriesGroupId))
                    {
                        yield return TmdbUtils.BaseTmdbUrl + $"tv/{externalId}/episode_group/{seriesGroupId}";
                    }
                }

                break;
            case Season season:
                var seasonSeries = season.Series;

                // The season number counts episode groups rather than TMDb seasons when the series is ordered by one.
                if (IsOrderedByEpisodeGroup(seasonSeries))
                {
                    if (seasonSeries.TryGetProviderId(MetadataProvider.Tmdb, out var groupSeriesId)
                        && seasonSeries.TryGetProviderId(TmdbEpisodeGroupId.ProviderKey, out var collectionId)
                        && season.TryGetProviderId(TmdbEpisodeGroupId.ProviderKey, out var groupId))
                    {
                        yield return TmdbUtils.BaseTmdbUrl + $"tv/{groupSeriesId}/episode_group/{collectionId}/group/{groupId}";
                    }

                    break;
                }

                // The season's own id already points at the right page, so prefer it over the numbering.
                if (season.TryGetProviderId(MetadataProvider.Tmdb, out var seasonExternalId))
                {
                    yield return TmdbUtils.BaseTmdbUrl + $"tv/season/{seasonExternalId}";
                    break;
                }

                if (seasonSeries?.TryGetProviderId(MetadataProvider.Tmdb, out var seriesExternalId) == true
                    && season.IndexNumber is { } seasonNumber)
                {
                    yield return TmdbUtils.BaseTmdbUrl + $"tv/{seriesExternalId}/season/{seasonNumber}";
                }

                break;
            case Episode episode:
                if (episode.TryGetProviderId(MetadataProvider.Tmdb, out var episodeExternalId))
                {
                    yield return TmdbUtils.BaseTmdbUrl + $"tv/episode/{episodeExternalId}";
                    break;
                }

                // Fall back to the numbering for items that were last refreshed before the episode id was stored.
                if (episode.Series?.TryGetProviderId(MetadataProvider.Tmdb, out seriesExternalId) == true
                    && episode.Season?.IndexNumber is { } episodeSeasonNumber
                    && episode.IndexNumber is { } episodeNumber
                    && !IsOrderedByEpisodeGroup(episode.Series))
                {
                    yield return TmdbUtils.BaseTmdbUrl + $"tv/{seriesExternalId}/season/{episodeSeasonNumber}/episode/{episodeNumber}";
                }

                break;
            case Movie:
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
            case BoxSet:
                if (item.TryGetProviderId(MetadataProvider.Tmdb, out externalId))
                {
                    yield return TmdbUtils.BaseTmdbUrl + $"collection/{externalId}";
                }

                break;
        }
    }

    // Episodes are looked up through the series' episode group whenever it has one, so its numbering replaces the TMDb one.
    private static bool IsOrderedByEpisodeGroup([NotNullWhen(true)] Series? series)
        => series?.TryGetProviderId(TmdbEpisodeGroupId.ProviderKey, out _) == true;
}

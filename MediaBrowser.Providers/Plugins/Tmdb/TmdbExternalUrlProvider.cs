using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using TMDbLib.Objects.TvShows;

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

                    if (!IsAirDateOrder(series.DisplayOrder)
                        && series.TryGetProviderId(TmdbUtils.EpisodeGroupProviderKey, out var seriesGroupId))
                    {
                        yield return TmdbUtils.BaseTmdbUrl + $"tv/{externalId}/episode_group/{seriesGroupId}";
                    }
                }

                break;
            case Season season:
                var seasonSeries = season.Series;

                // The season number counts episode groups rather than TMDb seasons when the series is ordered by one.
                if (seasonSeries is not null && !IsAirDateOrder(seasonSeries.DisplayOrder))
                {
                    if (seasonSeries.TryGetProviderId(MetadataProvider.Tmdb, out var groupSeriesId)
                        && seasonSeries.TryGetProviderId(TmdbUtils.EpisodeGroupProviderKey, out var collectionId)
                        && season.TryGetProviderId(TmdbUtils.EpisodeGroupProviderKey, out var groupId))
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
                    && IsAirDateOrder(episode.Series.DisplayOrder))
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

    // Only air date ordering keeps the Jellyfin season and episode numbers in sync with the TMDb ones,
    // every other episode group renumbers them.
    private static bool IsAirDateOrder(string? displayOrder)
        => string.IsNullOrEmpty(displayOrder) || TmdbUtils.GetEpisodeGroupType(displayOrder) == TvGroupType.OriginalAirDate;
}

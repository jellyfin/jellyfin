using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Plugins.Tmdb.TV;
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
            case Series:
                if (item.TryGetProviderId(MetadataProvider.Tmdb, out var externalId))
                {
                    yield return TmdbUtils.BaseTmdbUrl + $"tv/{externalId}";

                    if (item.TryGetProviderId(TmdbEpisodeGroupId.ProviderKey, out var episodeGroup))
                    {
                        yield return TmdbUtils.BaseTmdbUrl + $"tv/{externalId}/episode_group/{episodeGroup}";
                    }
                }

                break;
            case Season season:
                if (season.Series?.TryGetProviderId(MetadataProvider.Tmdb, out var seriesExternalId) == true)
                {
                    var orderString = season.Series.DisplayOrder;
                    var seasonNumber = season.IndexNumber;
                    if (string.IsNullOrEmpty(orderString) && seasonNumber is not null)
                    {
                        // Default order is airdate
                        yield return TmdbUtils.BaseTmdbUrl + $"tv/{seriesExternalId}/season/{seasonNumber}";
                    }

                    if (Enum.TryParse<TvGroupType>(season.Series.DisplayOrder, out var order))
                    {
                        if (order.Equals(TvGroupType.OriginalAirDate) && seasonNumber is not null)
                        {
                            yield return TmdbUtils.BaseTmdbUrl + $"tv/{seriesExternalId}/season/{seasonNumber}";
                        }
                    }

                    if (season.Series?.TryGetProviderId(TmdbEpisodeGroupId.ProviderKey, out var episodeGroup) == true
                        && season.TryGetProviderId(TmdbEpisodeGroupId.ProviderKey, out var group))
                    {
                        yield return TmdbUtils.BaseTmdbUrl + $"tv/{seriesExternalId}/episode_group/{episodeGroup}/group/{group}";
                    }
                }

                break;
            case Episode episode:
                if (episode.Series?.TryGetProviderId(MetadataProvider.Tmdb, out seriesExternalId) == true)
                {
                    var orderString = episode.Series.DisplayOrder;
                    var seasonNumber = episode.Season?.IndexNumber;
                    var episodeNumber = episode.IndexNumber;
                    if (string.IsNullOrEmpty(orderString) && seasonNumber is not null && episodeNumber is not null)
                    {
                        // Default order is airdate
                        yield return TmdbUtils.BaseTmdbUrl + $"tv/{seriesExternalId}/season/{seasonNumber}/episode/{episodeNumber}";
                    }

                    if (Enum.TryParse<TvGroupType>(orderString, out var order))
                    {
                        if (order.Equals(TvGroupType.OriginalAirDate) && seasonNumber is not null && episodeNumber is not null)
                        {
                            yield return TmdbUtils.BaseTmdbUrl + $"tv/{seriesExternalId}/season/{seasonNumber}/episode/{episodeNumber}";
                        }
                    }
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
}

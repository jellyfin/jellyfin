using System;
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
            case Series:
                if (item.TryGetProviderId(MetadataProvider.Tmdb, out var externalId))
                {
                    yield return TmdbUtils.BaseTmdbUrl + $"tv/{externalId}";
                }

                break;
            case Season season:
                if (season.Series.TryGetProviderId(MetadataProvider.Tmdb, out var seriesExternalId))
                {
                    var orderString = season.Series.DisplayOrder;
                    if (string.IsNullOrEmpty(orderString))
                    {
                        // Default order is airdate
                        yield return TmdbUtils.BaseTmdbUrl + $"tv/{seriesExternalId}/season/{season.IndexNumber}";
                    }

                    if (Enum.TryParse<TvGroupType>(season.Series.DisplayOrder, out var order))
                    {
                        if (order.Equals(TvGroupType.OriginalAirDate))
                        {
                            yield return TmdbUtils.BaseTmdbUrl + $"tv/{seriesExternalId}/season/{season.IndexNumber}";
                        }
                    }
                }

                break;
            case Episode episode:
                if (episode.Series.TryGetProviderId(MetadataProvider.Imdb, out seriesExternalId))
                {
                    var orderString = episode.Series.DisplayOrder;
                    if (string.IsNullOrEmpty(orderString))
                    {
                        // Default order is airdate
                        yield return TmdbUtils.BaseTmdbUrl + $"tv/{seriesExternalId}/season/{episode.Season.IndexNumber}/episode/{episode.IndexNumber}";
                    }

                    if (Enum.TryParse<TvGroupType>(orderString, out var order))
                    {
                        if (order.Equals(TvGroupType.OriginalAirDate))
                        {
                            yield return TmdbUtils.BaseTmdbUrl + $"tv/{seriesExternalId}/season/{episode.Season.IndexNumber}/episode/{episode.IndexNumber}";
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

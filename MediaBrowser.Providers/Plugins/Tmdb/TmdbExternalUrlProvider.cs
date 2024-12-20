using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

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
                var externalId = item.GetProviderId(MetadataProvider.Tmdb);
                if (!string.IsNullOrEmpty(externalId))
                {
                    yield return TmdbUtils.BaseTmdbUrl + $"tv/{externalId}";
                }

                break;
            case Season season:
                var seriesExternalId = season.Series.GetProviderId(MetadataProvider.Tmdb);
                if (!string.IsNullOrEmpty(seriesExternalId))
                {
                    yield return TmdbUtils.BaseTmdbUrl + $"tv/{seriesExternalId}/season/{season.IndexNumber}";
                }

                break;
            case Episode episode:
                seriesExternalId = episode.Series.GetProviderId(MetadataProvider.Tmdb);
                if (!string.IsNullOrEmpty(seriesExternalId))
                {
                    yield return TmdbUtils.BaseTmdbUrl + $"tv/{seriesExternalId}/season/{episode.Season.IndexNumber}/episode/{episode.IndexNumber}";
                }

                break;
            case Movie:
                externalId = item.GetProviderId(MetadataProvider.Tmdb);
                if (!string.IsNullOrEmpty(externalId))
                {
                    yield return TmdbUtils.BaseTmdbUrl + $"movie/{externalId}";
                }

                break;
            case Person:
                externalId = item.GetProviderId(MetadataProvider.Tmdb);
                if (!string.IsNullOrEmpty(externalId))
                {
                    yield return TmdbUtils.BaseTmdbUrl + $"person/{externalId}";
                }

                break;
            case BoxSet:
                externalId = item.GetProviderId(MetadataProvider.Tmdb);
                if (!string.IsNullOrEmpty(externalId))
                {
                    yield return TmdbUtils.BaseTmdbUrl + $"collection/{externalId}";
                }

                break;
        }
    }
}

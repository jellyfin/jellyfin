using System.Collections.Generic;
using System.Globalization;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.Tmdb.TV;

/// <summary>
/// External ID for a TMDB episode.
/// </summary>
public class TmdbEpisodeExternalId : IExternalId
{
    private const string Url = "https://www.themoviedb.org/tv/{0}/season/{1}/episode/{2}";

    /// <inheritdoc />
    public string ProviderName => TmdbUtils.ProviderName;

    /// <inheritdoc />
    public string Key => MetadataProvider.Tmdb.ToString();

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Episode;

    /// <inheritdoc />
    public string? UrlFormatString => null;

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Episode;

    /// <inheritdoc />
    public IEnumerable<ExternalUrl>? GetExternalUrls(IHasProviderIds item)
    {
        if (item is Episode episode
            && episode.IndexNumber != null
            && episode.ParentIndexNumber != null
            && episode.Series.TryGetProviderId(ProviderName, out var seriesId))
        {
            yield return new ExternalUrl(
                ProviderName,
                string.Format(
                    CultureInfo.InvariantCulture,
                    Url,
                    seriesId,
                    episode.ParentIndexNumber,
                    episode.IndexNumber));
        }
    }
}

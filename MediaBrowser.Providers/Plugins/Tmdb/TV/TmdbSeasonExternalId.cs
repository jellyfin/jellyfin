using System.Collections.Generic;
using System.Globalization;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.Tmdb.TV;

/// <summary>
/// External ID for a TMDB season.
/// </summary>
public class TmdbSeasonExternalId : IExternalId
{
    private const string Url = "https://www.themoviedb.org/tv/{0}/season/{1}";

    /// <inheritdoc />
    public string ProviderName => TmdbUtils.ProviderName;

    /// <inheritdoc />
    public string Key => MetadataProvider.Tmdb.ToString();

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Season;

    /// <inheritdoc />
    public string? UrlFormatString => null;

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Season;

    /// <inheritdoc />
    public IEnumerable<ExternalUrl>? GetExternalUrls(IHasProviderIds item)
    {
        if (item is Season season
            && season.IndexNumber != null
            && season.Series.TryGetProviderId(ProviderName, out var seriesId))
        {
            yield return new ExternalUrl(
                ProviderName,
                string.Format(
                    CultureInfo.InvariantCulture,
                    Url,
                    seriesId,
                    season.IndexNumber));
        }
    }
}

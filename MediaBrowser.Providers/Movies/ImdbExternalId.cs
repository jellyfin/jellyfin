using System.Collections.Generic;
using System.Globalization;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Movies;

/// <summary>
/// IMDb external id provider.
/// </summary>
public class ImdbExternalId : IExternalId
{
    private const string ImdbUrl = "https://www.imdb.com/title/{0}";
    private const string ImdbSeasonUrl = "https://www.imdb.com/title/{0}/episodes?season={1}";

    /// <inheritdoc />
    public string ProviderName => "IMDb";

    /// <inheritdoc />
    public string Key => MetadataProvider.Imdb.ToString();

    /// <inheritdoc />
    public ExternalIdMediaType? Type => null;

    /// <inheritdoc />
    public string? UrlFormatString => null;

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item)
    {
        // Supports images for tv movies
        if (item is LiveTvProgram tvProgram && tvProgram.IsMovie)
        {
            return true;
        }

        return item is Movie or MusicVideo or Series or Episode or Trailer;
    }

    /// <inheritdoc />
    public IEnumerable<ExternalUrl>? GetExternalUrls(IHasProviderIds item)
    {
        if (!item.TryGetProviderId(ProviderName, out var providerId))
        {
            yield break;
        }

        if (item is Season season)
        {
            if (season.IndexNumber > 0)
            {
                yield return new ExternalUrl(
                    ProviderName,
                    string.Format(CultureInfo.InvariantCulture, ImdbSeasonUrl, providerId, season.IndexNumber));
            }
        }
        else
        {
            yield return new ExternalUrl(
                ProviderName,
                string.Format(CultureInfo.InvariantCulture, ImdbUrl, providerId));
        }
    }
}

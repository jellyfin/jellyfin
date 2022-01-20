using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Movies;

/// <summary>
/// IMDb person external id provider.
/// </summary>
public class ImdbPersonExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => "IMDb";

    /// <inheritdoc />
    public string Key => MetadataProvider.Imdb.ToString();

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Person;

    /// <inheritdoc />
    public string? UrlFormatString => "https://www.imdb.com/name/{0}";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Person;

    /// <inheritdoc />
    public IEnumerable<ExternalUrl>? GetExternalUrls(IHasProviderIds item)
    {
        return null;
    }
}

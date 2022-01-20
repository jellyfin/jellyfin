using System.Collections.Generic;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.TV;

/// <summary>
/// zap2it external id provider.
/// </summary>
public class Zap2ItExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => "Zap2It";

    /// <inheritdoc />
    public string Key => MetadataProvider.Zap2It.ToString();

    /// <inheritdoc />
    public ExternalIdMediaType? Type => null;

    /// <inheritdoc />
    public string? UrlFormatString => "https://tvlistings.zap2it.com/overview.html?programSeriesId={0}";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Series;

    /// <inheritdoc />
    public IEnumerable<ExternalUrl>? GetExternalUrls(IHasProviderIds item)
    {
        return null;
    }
}

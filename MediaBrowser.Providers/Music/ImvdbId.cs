using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Music;

/// <summary>
/// IMVDb external id provider.
/// </summary>
public class ImvdbId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => "IMVDb";

    /// <inheritdoc />
    public string Key => "IMVDb";

    /// <inheritdoc />
    public ExternalIdMediaType? Type => null;

    /// <inheritdoc />
    public string? UrlFormatString => null;

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item)
        => item is MusicVideo;

    /// <inheritdoc />
    public IEnumerable<ExternalUrl>? GetExternalUrls(IHasProviderIds item)
    {
        return null;
    }
}

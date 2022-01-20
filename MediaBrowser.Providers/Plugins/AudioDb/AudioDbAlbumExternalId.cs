using System.Collections.Generic;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.AudioDb;

/// <summary>
/// AudioDb album external id provider.
/// </summary>
public class AudioDbAlbumExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => "TheAudioDb";

    /// <inheritdoc />
    public string Key => MetadataProvider.AudioDbAlbum.ToString();

    /// <inheritdoc />
    public ExternalIdMediaType? Type => null;

    /// <inheritdoc />
    public string? UrlFormatString => "https://www.theaudiodb.com/album/{0}";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is MusicAlbum;

    /// <inheritdoc />
    public IEnumerable<ExternalUrl>? GetExternalUrls(IHasProviderIds item)
    {
        return null;
    }
}

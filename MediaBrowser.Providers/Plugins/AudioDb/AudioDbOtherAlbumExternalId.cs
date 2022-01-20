using System.Collections.Generic;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.AudioDb;

/// <summary>
/// AudioDb other album external id provider.
/// </summary>
public class AudioDbOtherAlbumExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => "TheAudioDb";

    /// <inheritdoc />
    public string Key => MetadataProvider.AudioDbAlbum.ToString();

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Album;

    /// <inheritdoc />
    public string? UrlFormatString => "https://www.theaudiodb.com/album/{0}";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Audio;

    /// <inheritdoc />
    public IEnumerable<ExternalUrl>? GetExternalUrls(IHasProviderIds item)
    {
        return null;
    }
}

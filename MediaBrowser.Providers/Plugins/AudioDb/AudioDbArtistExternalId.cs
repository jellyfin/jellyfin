using System.Collections.Generic;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.AudioDb;

/// <summary>
/// AudioDb artist external id provider.
/// </summary>
public class AudioDbArtistExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => "TheAudioDb";

    /// <inheritdoc />
    public string Key => MetadataProvider.AudioDbArtist.ToString();

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Artist;

    /// <inheritdoc />
    public string? UrlFormatString => "https://www.theaudiodb.com/artist/{0}";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is MusicArtist;


    /// <inheritdoc />
    public IEnumerable<ExternalUrl>? GetExternalUrls(IHasProviderIds item)
    {
        return null;
    }
}

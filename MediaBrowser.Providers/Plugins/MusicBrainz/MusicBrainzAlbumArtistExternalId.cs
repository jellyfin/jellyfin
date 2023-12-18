using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// MusicBrainz album artist external id.
/// </summary>
public class MusicBrainzAlbumArtistExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => "MusicBrainz";

    /// <inheritdoc />
    public string Key => MetadataProvider.MusicBrainzAlbumArtist.ToString();

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.AlbumArtist;

    /// <inheritdoc />
    public string UrlFormatString => Plugin.Instance!.Configuration.Server + "/artist/{0}";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Audio;
}

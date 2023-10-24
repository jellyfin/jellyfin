using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// MusicBrainz album external id.
/// </summary>
public class MusicBrainzAlbumExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => "MusicBrainz";

    /// <inheritdoc />
    public string Key => MetadataProvider.MusicBrainzAlbum.ToString();

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Album;

    /// <inheritdoc />
    public string UrlFormatString => Plugin.Instance!.Configuration.Server + "/release/{0}";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Audio || item is MusicAlbum;
}

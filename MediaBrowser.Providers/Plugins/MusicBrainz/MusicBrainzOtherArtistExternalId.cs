using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// MusicBrainz other artist external id.
/// </summary>
public class MusicBrainzOtherArtistExternalId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => "MusicBrainz";

    /// <inheritdoc />
    public string Key => MetadataProvider.MusicBrainzArtist.ToString();

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.OtherArtist;

    /// <inheritdoc />
    public string UrlFormatString => Plugin.Instance!.Configuration.Server + "/artist/{0}";

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Audio or MusicAlbum;
}

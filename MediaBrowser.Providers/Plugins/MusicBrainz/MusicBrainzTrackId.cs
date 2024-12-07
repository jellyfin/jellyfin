using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// MusicBrainz track id.
/// </summary>
public class MusicBrainzTrackId : IExternalId
{
    /// <inheritdoc />
    public string ProviderName => "MusicBrainz";

    /// <inheritdoc />
    public string Key => MetadataProvider.MusicBrainzTrack.ToString();

    /// <inheritdoc />
    public ExternalIdMediaType? Type => ExternalIdMediaType.Track;

    /// <inheritdoc />
    public bool Supports(IHasProviderIds item) => item is Audio;
}

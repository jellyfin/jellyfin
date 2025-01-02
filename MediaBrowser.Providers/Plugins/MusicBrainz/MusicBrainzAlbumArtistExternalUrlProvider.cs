using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// External album artist URLs for MusicBrainz.
/// </summary>
public class MusicBrainzAlbumArtistExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => "MusicBrainz Album Artist";

    /// <inheritdoc/>
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item is MusicAlbum)
        {
            var externalId = item.GetProviderId(MetadataProvider.MusicBrainzAlbumArtist);
            if (!string.IsNullOrEmpty(externalId))
            {
                yield return Plugin.Instance!.Configuration.Server + $"/artist/{externalId}";
            }
        }
    }
}

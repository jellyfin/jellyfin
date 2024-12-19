using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// External album URLs for MusicBrainz.
/// </summary>
public class MusicBrainzAlbumExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => "MusicBrainz Album";

    /// <inheritdoc/>
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item is MusicAlbum)
        {
            var externalId = item.GetProviderId(MetadataProvider.MusicBrainzArtist);
            if (!string.IsNullOrEmpty(externalId))
            {
                yield return Plugin.Instance!.Configuration.Server + $"/release/{externalId}";
            }
        }
    }
}

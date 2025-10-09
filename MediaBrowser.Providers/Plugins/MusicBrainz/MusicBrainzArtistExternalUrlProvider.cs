using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// External artist URLs for MusicBrainz.
/// </summary>
public class MusicBrainzArtistExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => "MusicBrainz Artist";

    /// <inheritdoc/>
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item.TryGetProviderId(MetadataProvider.MusicBrainzArtist, out var externalId))
        {
            switch (item)
            {
                case MusicArtist:
                case Person:
                    yield return Plugin.Instance!.Configuration.Server + $"/artist/{externalId}";

                    break;
            }
        }
    }
}

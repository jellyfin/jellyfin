using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// External release group URLs for MusicBrainz.
/// </summary>
public class MusicBrainzReleaseGroupExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => "MusicBrainz Release Group";

    /// <inheritdoc/>
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item is MusicAlbum)
        {
        if (item.TryGetProviderId(MetadataProvider.MusicBrainzReleaseGroup, out var externalId))
            {
                yield return Plugin.Instance!.Configuration.Server + $"/release-group/{externalId}";
            }
        }
    }
}

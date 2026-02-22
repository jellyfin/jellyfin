using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Plugins.MusicBrainz;

/// <summary>
/// External track URLs for MusicBrainz.
/// </summary>
public class MusicBrainzTrackExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => "MusicBrainz Track";

    /// <inheritdoc/>
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        if (item is Audio)
        {
        if (item.TryGetProviderId(MetadataProvider.MusicBrainzTrack, out var externalId))
            {
                yield return Plugin.Instance!.Configuration.Server + $"/track/{externalId}";
            }
        }
    }
}

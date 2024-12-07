using System.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Providers.Plugins.AudioDb;

/// <summary>
/// External artist URLs for AudioDb.
/// </summary>
public class AudioDbAlbumExternalUrlProvider : IExternalUrlProvider
{
    /// <inheritdoc/>
    public string Name => "TheAudioDb Album";

    /// <inheritdoc/>
    public IEnumerable<string> GetExternalUrls(BaseItem item)
    {
        var externalId = item.GetProviderId(MetadataProvider.AudioDbAlbum);
        if (!string.IsNullOrEmpty(externalId))
        {
            var baseUrl = "https://www.theaudiodb.com/";
            switch (item)
            {
                case MusicAlbum:
                    yield return baseUrl + $"album/{externalId}";
                    break;
            }
        }
    }
}

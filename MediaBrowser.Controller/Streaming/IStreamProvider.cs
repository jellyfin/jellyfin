using System.IO;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Controller.Streaming;

/// <summary>
/// Defines a provider that can open a media stream.
/// </summary>
public interface IStreamProvider
{
    /// <summary>
    /// Gets the stream provider name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the stream protocol supported by this provider.
    /// </summary>
    MediaStreamProtocol StreamProtocol { get; }

    /// <summary>
    /// Returns whether this provider supports a given file.
    /// </summary>
    /// <param name="item">The media item to evaluate.</param>
    /// <returns>Returns <c>true</c> if supported.</returns>
    bool Supports(BaseItem item);

    /// <summary>
    /// Opens a stream for the requested item.
    /// </summary>
    /// <param name="playbackInfo">Playback info for the requested item.</param>
    /// <returns>Returns an open stream.</returns>
    Task<Stream> Stream(PlaybackInfoResponse playbackInfo);
}

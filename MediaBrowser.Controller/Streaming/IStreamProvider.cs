using System.IO;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Streaming;

/// <summary>
/// Defines a provider that can open a media stream.
/// </summary>
public interface IStreamProvider
{
    /// <summary>
    /// Gets the output container of the generated stream.
    /// </summary>
    string StreamContainer { get; }

    /// <summary>
    /// Gets the supported stream protocol.
    /// </summary>
    MediaStreamProtocol StreamProtocol { get; }

    /// <summary>
    /// Returns whether this provider supports the source item and the device profile.
    /// </summary>
    /// <param name="item">The source media item to evaluate.</param>
    /// <param name="profile">The device profile for this request.</param>
    /// <returns>Returns <c>true</c> if supported.</returns>
    bool Supports(BaseItem item, DeviceProfile profile);

    /// <summary>
    /// Opens a stream for the requested media source.
    /// </summary>
    /// <param name="mediaSource">The negotiated media source.</param>
    /// <param name="playSessionId">The play session ID.</param>
    /// <returns>Returns an open stream.</returns>
    Task<Stream> Stream(MediaSourceInfo mediaSource, string playSessionId);
}

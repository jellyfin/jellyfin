using System.ComponentModel;

namespace Jellyfin.Data.Enums;

/// <summary>
/// Media streaming protocol.
/// </summary>
[DefaultValue(Http)]
public enum MediaStreamProtocol
{
    /// <summary>
    /// HTTP.
    /// </summary>
    Http = 0,

    /// <summary>
    /// HTTP Live Streaming.
    /// </summary>
    Hls = 1
}

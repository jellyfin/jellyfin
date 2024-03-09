#pragma warning disable SA1300 // Lowercase required for backwards compat.
using System.ComponentModel;

namespace Jellyfin.Data.Enums;

/// <summary>
/// Media streaming protocol.
/// Lowercase for backwards compatibility.
/// </summary>
[DefaultValue(http)]
public enum MediaStreamProtocol
{
    /// <summary>
    /// HTTP.
    /// </summary>
    http = 0,

    /// <summary>
    /// HTTP Live Streaming.
    /// </summary>
    hls = 1
}

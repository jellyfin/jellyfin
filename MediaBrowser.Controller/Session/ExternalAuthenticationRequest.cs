using System;

namespace MediaBrowser.Controller.Session;

/// <summary>
/// Request to create a Jellyfin session for a user already authenticated by an external provider.
/// </summary>
public class ExternalAuthenticationRequest
{
    /// <summary>
    /// Gets or sets the Jellyfin user id.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the client name.
    /// </summary>
    public string App { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the client version.
    /// </summary>
    public string AppVersion { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device id.
    /// </summary>
    public string DeviceId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the device name.
    /// </summary>
    public string DeviceName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the remote endpoint.
    /// </summary>
    public string RemoteEndPoint { get; set; } = string.Empty;
}

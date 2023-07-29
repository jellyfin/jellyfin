using System;
using MediaBrowser.Controller.Session;

namespace MediaBrowser.Controller.Events.Authentication;

/// <summary>
/// A class representing an authentication result event.
/// </summary>
public class AuthenticationRequestEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationRequestEventArgs"/> class.
    /// </summary>
    /// <param name="request">The <see cref="AuthenticationRequest"/>.</param>
    public AuthenticationRequestEventArgs(AuthenticationRequest request)
    {
        Username = request.Username;
        UserId = request.UserId;
        App = request.App;
        AppVersion = request.AppVersion;
        DeviceId = request.DeviceId;
        DeviceName = request.DeviceName;
        RemoteEndPoint = request.RemoteEndPoint;
    }

    /// <summary>
    /// Gets or sets the user name.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the user id.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>
    /// Gets or sets the app.
    /// </summary>
    public string? App { get; set; }

    /// <summary>
    /// Gets or sets the app version.
    /// </summary>
    public string? AppVersion { get; set; }

    /// <summary>
    /// Gets or sets the device id.
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// Gets or sets the device name.
    /// </summary>
    public string? DeviceName { get; set; }

    /// <summary>
    /// Gets or sets the remote endpoint.
    /// </summary>
    public string? RemoteEndPoint { get; set; }
}

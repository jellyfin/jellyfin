using System;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Events.Authentication;

/// <summary>
/// A class representing an authentication result event.
/// </summary>
public class AuthenticationResultEventArgs : EventArgs
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuthenticationResultEventArgs"/> class.
    /// </summary>
    /// <param name="result">The <see cref="AuthenticationResult"/>.</param>
    public AuthenticationResultEventArgs(AuthenticationResult result)
    {
        User = result.User;
        SessionInfo = result.SessionInfo;
        ServerId = result.ServerId;
    }

    /// <summary>
    /// Gets or sets the user.
    /// </summary>
    public UserDto User { get; set; }

    /// <summary>
    /// Gets or sets the session information.
    /// </summary>
    public SessionInfoDto? SessionInfo { get; set; }

    /// <summary>
    /// Gets or sets the server id.
    /// </summary>
    public string? ServerId { get; set; }
}

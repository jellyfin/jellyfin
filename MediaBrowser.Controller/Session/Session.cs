#nullable disable

using MediaBrowser;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Session;

/// <summary>
/// A class representing a session.
/// </summary>
public class Session
{
    /// <summary>
    /// Gets or sets the user.
    /// </summary>
    public UserDto User { get; set; }

    /// <summary>
    /// Gets or sets the session info.
    /// </summary>
    public SessionInfoDto SessionInfo { get; set; }

    /// <summary>
    /// Gets or sets the access token.
    /// </summary>
    public string AccessToken { get; set; }

    /// <summary>
    /// Gets or sets the server id.
    /// </summary>
    public string ServerId { get; set; }
}

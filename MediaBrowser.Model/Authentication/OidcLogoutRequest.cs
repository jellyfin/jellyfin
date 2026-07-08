namespace MediaBrowser.Model.Authentication;

/// <summary>
/// Request to log out an OpenID Connect backed Jellyfin session.
/// </summary>
public class OidcLogoutRequest
{
    /// <summary>
    /// Gets or sets the relative URL to return to after provider logout.
    /// </summary>
    public string? ReturnUrl { get; set; }
}

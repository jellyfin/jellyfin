namespace MediaBrowser.Model.Authentication;

/// <summary>
/// OpenID Connect logout result.
/// </summary>
public class OidcLogoutResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the Jellyfin session was revoked.
    /// </summary>
    public bool LocalSessionRevoked { get; set; }

    /// <summary>
    /// Gets or sets the upstream provider logout URL, when available.
    /// </summary>
    public string? ExternalLogoutUrl { get; set; }
}

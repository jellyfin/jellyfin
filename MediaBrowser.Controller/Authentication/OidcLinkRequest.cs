using System;

namespace MediaBrowser.Controller.Authentication;

/// <summary>
/// OpenID Connect account-linking state for an authenticated Jellyfin user.
/// </summary>
public class OidcLinkRequest
{
    /// <summary>
    /// Gets or sets the provider id.
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Jellyfin user id.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Gets or sets the relative return URL.
    /// </summary>
    public string? ReturnUrl { get; set; }
}

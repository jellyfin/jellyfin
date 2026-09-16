namespace MediaBrowser.Model.Authentication;

/// <summary>
/// Defines how an OpenID Connect identity can be linked to a Jellyfin user.
/// </summary>
public enum OidcUserProvisioningMode
{
    /// <summary>
    /// Only pre-linked external identities can authenticate.
    /// </summary>
    Disabled = 0,

    /// <summary>
    /// Create a new Jellyfin user when no matching user exists.
    /// </summary>
    CreateUser = 1
}

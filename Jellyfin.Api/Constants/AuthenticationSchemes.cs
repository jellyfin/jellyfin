namespace Jellyfin.Api.Constants;

/// <summary>
/// Authentication schemes for user authentication in the API.
/// </summary>
public static class AuthenticationSchemes
{
    /// <summary>
    /// Scheme name for the custom legacy authentication.
    /// </summary>
    public const string CustomAuthentication = "CustomAuthentication";

    /// <summary>
    /// Scheme name for the transient OpenID Connect external sign-in cookie.
    /// </summary>
    public const string OidcExternalCookie = "OidcExternalCookie";

    /// <summary>
    /// Authentication property key for the OpenID Connect id token.
    /// </summary>
    public const string OidcIdTokenProperty = "jellyfin:id_token";

    /// <summary>
    /// Gets the OpenID Connect scheme name for a provider.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <returns>The OpenID Connect scheme name.</returns>
    public static string GetOidcScheme(string providerId)
    {
        return "Oidc:" + providerId;
    }

    /// <summary>
    /// Gets the OpenID Connect external cookie scheme name for a provider.
    /// </summary>
    /// <param name="providerId">The provider id.</param>
    /// <returns>The OpenID Connect external cookie scheme name.</returns>
    public static string GetOidcExternalCookieScheme(string providerId)
    {
        return OidcExternalCookie + ":" + providerId;
    }
}

namespace Jellyfin.Api.Constants;

/// <summary>
/// Constants for OpenID Connect authentication flows.
/// </summary>
public static class OidcConstants
{
    /// <summary>
    /// Authentication property for the browser return URL.
    /// </summary>
    public const string ReturnUrlProperty = "jellyfin:returnUrl";

    /// <summary>
    /// Query string parameter for OpenID Connect errors returned to a local page.
    /// </summary>
    public const string ErrorQueryParameter = "oidc_error";

    /// <summary>
    /// Error value for remote OpenID Connect failures.
    /// </summary>
    public const string RemoteFailureError = "remote_failure";

    /// <summary>
    /// Error value for local OpenID Connect validation failures.
    /// </summary>
    public const string LocalFailureError = "local_failure";

    /// <summary>
    /// Returns a value indicating whether a URL is a safe relative browser return URL.
    /// </summary>
    /// <param name="url">The return URL.</param>
    /// <returns><c>true</c> when the URL is a safe relative path.</returns>
    public static bool IsSafeRelativeUrl(string url)
    {
        return url.Length > 0
               && url[0] == '/'
               && (url.Length == 1 || (url[1] != '/' && url[1] != '\\'));
    }
}

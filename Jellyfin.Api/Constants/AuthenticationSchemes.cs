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
    /// Scheme name for the playback grant authentication.
    /// </summary>
    public const string PlaybackAccess = "PlaybackAccess";
}

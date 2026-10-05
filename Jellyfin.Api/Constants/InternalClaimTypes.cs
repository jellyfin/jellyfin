namespace Jellyfin.Api.Constants;

/// <summary>
/// Internal claim types for authorization.
/// </summary>
public static class InternalClaimTypes
{
    /// <summary>
    /// User Id.
    /// </summary>
    public const string UserId = "Jellyfin-UserId";

    /// <summary>
    /// Device Id.
    /// </summary>
    public const string DeviceId = "Jellyfin-DeviceId";

    /// <summary>
    /// Device.
    /// </summary>
    public const string Device = "Jellyfin-Device";

    /// <summary>
    /// Client.
    /// </summary>
    public const string Client = "Jellyfin-Client";

    /// <summary>
    /// Version.
    /// </summary>
    public const string Version = "Jellyfin-Version";

    /// <summary>
    /// Token.
    /// </summary>
    public const string Token = "Jellyfin-Token";

    /// <summary>
    /// Is Api Key.
    /// </summary>
    public const string IsApiKey = "Jellyfin-IsApiKey";

    /// <summary>
    /// The scoped playback credential. This is never an account token.
    /// </summary>
    public const string PlaybackToken = "Jellyfin-PlaybackToken";

    /// <summary>
    /// The playback session authorized by the credential.
    /// </summary>
    public const string PlaybackSessionId = "Jellyfin-PlaybackSessionId";
}

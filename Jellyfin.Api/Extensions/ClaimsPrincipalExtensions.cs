using System;
using System.Linq;
using System.Security.Claims;
using Jellyfin.Api.Constants;

namespace Jellyfin.Api.Extensions;

/// <summary>
/// Extensions for <see cref="ClaimsPrincipal"/>.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Get user id from claims.
    /// </summary>
    /// <param name="user">Current claims principal.</param>
    /// <returns>User id.</returns>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var value = GetClaimValue(user, InternalClaimTypes.UserId);
        return string.IsNullOrEmpty(value)
            ? default
            : Guid.Parse(value);
    }

    /// <summary>
    /// Get device id from claims.
    /// </summary>
    /// <param name="user">Current claims principal.</param>
    /// <returns>Device id.</returns>
    public static string? GetDeviceId(this ClaimsPrincipal user)
        => GetClaimValue(user, InternalClaimTypes.DeviceId);

    /// <summary>
    /// Get device from claims.
    /// </summary>
    /// <param name="user">Current claims principal.</param>
    /// <returns>Device.</returns>
    public static string? GetDevice(this ClaimsPrincipal user)
        => GetClaimValue(user, InternalClaimTypes.Device);

    /// <summary>
    /// Get client from claims.
    /// </summary>
    /// <param name="user">Current claims principal.</param>
    /// <returns>Client.</returns>
    public static string? GetClient(this ClaimsPrincipal user)
        => GetClaimValue(user, InternalClaimTypes.Client);

    /// <summary>
    /// Get version from claims.
    /// </summary>
    /// <param name="user">Current claims principal.</param>
    /// <returns>Version.</returns>
    public static string? GetVersion(this ClaimsPrincipal user)
        => GetClaimValue(user, InternalClaimTypes.Version);

    /// <summary>
    /// Get token from claims.
    /// </summary>
    /// <param name="user">Current claims principal.</param>
    /// <returns>Token.</returns>
    public static string? GetToken(this ClaimsPrincipal user)
        => GetClaimValue(user, InternalClaimTypes.Token);

    /// <summary>
    /// Gets a flag specifying whether the request is using an api key.
    /// </summary>
    /// <param name="user">Current claims principal.</param>
    /// <returns>The flag specifying whether the request is using an api key.</returns>
    public static bool GetIsApiKey(this ClaimsPrincipal user)
    {
        var claimValue = GetClaimValue(user, InternalClaimTypes.IsApiKey);
        return bool.TryParse(claimValue, out var parsedClaimValue)
               && parsedClaimValue;
    }

    /// <summary>
    /// Builds the credential query for child media resources without converting a
    /// playback credential into a general account credential.
    /// </summary>
    /// <param name="user">The current principal.</param>
    /// <returns>The media authorization query, without a leading question mark.</returns>
    public static string GetMediaAuthorizationQuery(this ClaimsPrincipal user)
    {
        var playbackToken = GetClaimValue(user, InternalClaimTypes.PlaybackToken);
        if (!string.IsNullOrEmpty(playbackToken))
        {
            return "PlaybackToken=" + Uri.EscapeDataString(playbackToken)
                + "&PlaySessionId=" + Uri.EscapeDataString(GetClaimValue(user, InternalClaimTypes.PlaybackSessionId) ?? string.Empty);
        }

        return "ApiKey=" + Uri.EscapeDataString(user.GetToken() ?? string.Empty);
    }

    private static string? GetClaimValue(in ClaimsPrincipal user, string name)
        => user.Claims.FirstOrDefault(claim => claim.Type.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;
}

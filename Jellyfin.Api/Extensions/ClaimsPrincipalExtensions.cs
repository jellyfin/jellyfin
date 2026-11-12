using System;
using System.Globalization;
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
    /// Gets a flag specifying whether the request is authenticated with a share link.
    /// </summary>
    /// <param name="user">Current claims principal.</param>
    /// <returns>The flag specifying whether the request is authenticated with a share link.</returns>
    public static bool GetIsShareLink(this ClaimsPrincipal user)
    {
        var claimValue = GetClaimValue(user, InternalClaimTypes.IsShareLink);
        return bool.TryParse(claimValue, out var parsedClaimValue)
               && parsedClaimValue;
    }

    /// <summary>
    /// Get share link item id from claims.
    /// </summary>
    /// <param name="user">Current claims principal.</param>
    /// <returns>The share link item id, or <see cref="Guid.Empty"/> when the claim is absent.</returns>
    public static Guid GetShareItemId(this ClaimsPrincipal user)
    {
        var value = GetClaimValue(user, InternalClaimTypes.ShareItemId);
        return string.IsNullOrEmpty(value)
            ? default
            : Guid.Parse(value);
    }

    /// <summary>
    /// Get share link token id (jti) from claims.
    /// </summary>
    /// <param name="user">Current claims principal.</param>
    /// <returns>The share link token id, or <c>null</c> when the claim is absent.</returns>
    public static string? GetShareJti(this ClaimsPrincipal user)
        => GetClaimValue(user, InternalClaimTypes.ShareJti);

    /// <summary>
    /// Get share link absolute expiration from claims.
    /// </summary>
    /// <param name="user">Current claims principal.</param>
    /// <returns>The absolute expiration in unix seconds, or <c>null</c> when the claim is absent.</returns>
    public static long? GetShareExpiresAt(this ClaimsPrincipal user)
    {
        var value = GetClaimValue(user, InternalClaimTypes.ShareExpiresAt);
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    /// <summary>
    /// Get share link duration from claims.
    /// </summary>
    /// <param name="user">Current claims principal.</param>
    /// <returns>The duration in seconds, or <c>null</c> when the claim is absent.</returns>
    public static int? GetShareDurationSeconds(this ClaimsPrincipal user)
    {
        var value = GetClaimValue(user, InternalClaimTypes.ShareDurationSeconds);
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : null;
    }

    private static string? GetClaimValue(in ClaimsPrincipal user, string name)
        => user.Claims.FirstOrDefault(claim => claim.Type.Equals(name, StringComparison.OrdinalIgnoreCase))?.Value;
}

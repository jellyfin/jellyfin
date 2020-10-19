using System;
using System.Linq;
using System.Security.Claims;
using Jellyfin.Api.Constants;

namespace Jellyfin.Api.Helpers
{
    /// <summary>
    /// Claim Helpers.
    /// </summary>
    public static class ClaimHelpers
    {
        /// <summary>
        /// Get user id from claims.
        /// </summary>
        /// <param name="user">Current claims principal.</param>
        /// <returns>User id.</returns>
        public static Guid? GetUserId(in ClaimsPrincipal user)
        {
            var value = GetClaimValue(user, InternalClaimTypes.UserId);
            return string.IsNullOrEmpty(value)
                ? null
                : (Guid?)Guid.Parse(value);
        }

        /// <summary>
        /// Get device id from claims.
        /// </summary>
        /// <param name="user">Current claims principal.</param>
        /// <returns>Device id.</returns>
        public static string? GetDeviceId(in ClaimsPrincipal user)
            => GetClaimValue(user, InternalClaimTypes.DeviceId);

        /// <summary>
        /// Get device from claims.
        /// </summary>
        /// <param name="user">Current claims principal.</param>
        /// <returns>Device.</returns>
        public static string? GetDevice(in ClaimsPrincipal user)
            => GetClaimValue(user, InternalClaimTypes.Device);

        /// <summary>
        /// Get client from claims.
        /// </summary>
        /// <param name="user">Current claims principal.</param>
        /// <returns>Client.</returns>
        public static string? GetClient(in ClaimsPrincipal user)
            => GetClaimValue(user, InternalClaimTypes.Client);

        /// <summary>
        /// Get version from claims.
        /// </summary>
        /// <param name="user">Current claims principal.</param>
        /// <returns>Version.</returns>
        public static string? GetVersion(in ClaimsPrincipal user)
            => GetClaimValue(user, InternalClaimTypes.Version);

        /// <summary>
        /// Get token from claims.
        /// </summary>
        /// <param name="user">Current claims principal.</param>
        /// <returns>Token.</returns>
        public static string? GetToken(in ClaimsPrincipal user)
            => GetClaimValue(user, InternalClaimTypes.Token);

        private static string? GetClaimValue(in ClaimsPrincipal user, string name)
        {
            return user?.Identities
                .SelectMany(c => c.Claims)
                .Where(claim => claim.Type.Equals(name, StringComparison.OrdinalIgnoreCase))
                .Select(claim => claim.Value)
                .FirstOrDefault();
        }
    }
}

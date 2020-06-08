using System;
using System.Linq;
using Jellyfin.Api.Constants;
using Microsoft.AspNetCore.Http;

#nullable enable

namespace Jellyfin.Api.Extensions
{
    /// <summary>
    /// Claim Helpers.
    /// </summary>
    public static class ClaimHelpers
    {
        /// <summary>
        /// Get user id from claims.
        /// </summary>
        /// <param name="request">Current request.</param>
        /// <returns>User id.</returns>
        public static Guid? GetUserId(in HttpRequest request)
        {
            var value = GetClaimValue(request, InternalClaimTypes.UserId);
            return string.IsNullOrEmpty(value)
                ? null
                : (Guid?)Guid.Parse(value);
        }

        /// <summary>
        /// Get device id from claims.
        /// </summary>
        /// <param name="request">Current request.</param>
        /// <returns>Device id.</returns>
        public static string? GetDeviceId(in HttpRequest request)
            => GetClaimValue(request, InternalClaimTypes.DeviceId);

        /// <summary>
        /// Get device from claims.
        /// </summary>
        /// <param name="request">Current request.</param>
        /// <returns>Device.</returns>
        public static string? GetDevice(in HttpRequest request)
            => GetClaimValue(request, InternalClaimTypes.Device);

        /// <summary>
        /// Get client from claims.
        /// </summary>
        /// <param name="request">Current request.</param>
        /// <returns>Client.</returns>
        public static string? GetClient(in HttpRequest request)
            => GetClaimValue(request, InternalClaimTypes.Client);

        /// <summary>
        /// Get version from claims.
        /// </summary>
        /// <param name="request">Current request.</param>
        /// <returns>Version.</returns>
        public static string? GetVersion(in HttpRequest request)
            => GetClaimValue(request, InternalClaimTypes.Version);

        /// <summary>
        /// Get token from claims.
        /// </summary>
        /// <param name="request">Current request.</param>
        /// <returns>Token.</returns>
        public static string? GetToken(in HttpRequest request)
            => GetClaimValue(request, InternalClaimTypes.Token);

        private static string? GetClaimValue(in HttpRequest request, string name)
        {
            return request?.HttpContext.User.Identities
                .SelectMany(c => c.Claims)
                .Where(claim => claim.Type.Equals(name, StringComparison.OrdinalIgnoreCase))
                .Select(claim => claim.Value)
                .FirstOrDefault();
        }
    }
}

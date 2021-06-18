#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Server.Implementations.Security
{
    public class AuthorizationContext : IAuthorizationContext
    {
        private readonly JellyfinDbProvider _jellyfinDbProvider;
        private readonly IUserManager _userManager;

        public AuthorizationContext(JellyfinDbProvider jellyfinDb, IUserManager userManager)
        {
            _jellyfinDbProvider = jellyfinDb;
            _userManager = userManager;
        }

        public Task<AuthorizationInfo> GetAuthorizationInfo(HttpContext requestContext)
        {
            if (requestContext.Request.HttpContext.Items.TryGetValue("AuthorizationInfo", out var cached) && cached != null)
            {
                return Task.FromResult((AuthorizationInfo)cached!); // Cache should never contain null
            }

            return GetAuthorization(requestContext);
        }

        public async Task<AuthorizationInfo> GetAuthorizationInfo(HttpRequest requestContext)
        {
            var auth = GetAuthorizationDictionary(requestContext);
            var authInfo = await GetAuthorizationInfoFromDictionary(auth, requestContext.Headers, requestContext.Query).ConfigureAwait(false);
            return authInfo;
        }

        /// <summary>
        /// Gets the authorization.
        /// </summary>
        /// <param name="httpReq">The HTTP req.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private async Task<AuthorizationInfo> GetAuthorization(HttpContext httpReq)
        {
            var auth = GetAuthorizationDictionary(httpReq);
            var authInfo = await GetAuthorizationInfoFromDictionary(auth, httpReq.Request.Headers, httpReq.Request.Query).ConfigureAwait(false);

            httpReq.Request.HttpContext.Items["AuthorizationInfo"] = authInfo;
            return authInfo;
        }

        private async Task<AuthorizationInfo> GetAuthorizationInfoFromDictionary(
            IReadOnlyDictionary<string, string>? auth,
            IHeaderDictionary headers,
            IQueryCollection queryString)
        {
            string? deviceId = null;
            string? deviceName = null;
            string? client = null;
            string? version = null;
            string? token = null;

            if (auth != null)
            {
                auth.TryGetValue("DeviceId", out deviceId);
                auth.TryGetValue("Device", out deviceName);
                auth.TryGetValue("Client", out client);
                auth.TryGetValue("Version", out version);
                auth.TryGetValue("Token", out token);
            }

#pragma warning disable CA1508
            // headers can return StringValues.Empty
            if (string.IsNullOrEmpty(token))
            {
                token = headers["X-Emby-Token"];
            }

            if (string.IsNullOrEmpty(token))
            {
                token = headers["X-MediaBrowser-Token"];
            }

            if (string.IsNullOrEmpty(token))
            {
                token = queryString["ApiKey"];
            }

            // TODO deprecate this query parameter.
            if (string.IsNullOrEmpty(token))
            {
                token = queryString["api_key"];
            }

            var authInfo = new AuthorizationInfo
            {
                Client = client,
                Device = deviceName,
                DeviceId = deviceId,
                Version = version,
                Token = token,
                IsAuthenticated = false,
                HasToken = false
            };

            if (string.IsNullOrWhiteSpace(token))
            {
                // Request doesn't contain a token.
                return authInfo;
            }
#pragma warning restore CA1508

            authInfo.HasToken = true;
            await using var dbContext = _jellyfinDbProvider.CreateContext();
            var device = await dbContext.Devices.FirstOrDefaultAsync(d => d.AccessToken == token).ConfigureAwait(false);

            if (device != null)
            {
                authInfo.IsAuthenticated = true;
                var updateToken = false;

                // TODO: Remove these checks for IsNullOrWhiteSpace
                if (string.IsNullOrWhiteSpace(authInfo.Client))
                {
                    authInfo.Client = device.AppName;
                }

                if (string.IsNullOrWhiteSpace(authInfo.DeviceId))
                {
                    authInfo.DeviceId = device.DeviceId;
                }

                // Temporary. TODO - allow clients to specify that the token has been shared with a casting device
                var allowTokenInfoUpdate = !authInfo.Client.Contains("chromecast", StringComparison.OrdinalIgnoreCase);

                if (string.IsNullOrWhiteSpace(authInfo.Device))
                {
                    authInfo.Device = device.DeviceName;
                }
                else if (!string.Equals(authInfo.Device, device.DeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    if (allowTokenInfoUpdate)
                    {
                        updateToken = true;
                        device.DeviceName = authInfo.Device;
                    }
                }

                if (string.IsNullOrWhiteSpace(authInfo.Version))
                {
                    authInfo.Version = device.AppVersion;
                }
                else if (!string.Equals(authInfo.Version, device.AppVersion, StringComparison.OrdinalIgnoreCase))
                {
                    if (allowTokenInfoUpdate)
                    {
                        updateToken = true;
                        device.AppVersion = authInfo.Version;
                    }
                }

                if ((DateTime.UtcNow - device.DateLastActivity).TotalMinutes > 3)
                {
                    device.DateLastActivity = DateTime.UtcNow;
                    updateToken = true;
                }

                if (!device.UserId.Equals(Guid.Empty))
                {
                    authInfo.User = _userManager.GetUserById(device.UserId);
                    authInfo.IsApiKey = false;
                }
                else
                {
                    authInfo.IsApiKey = true;
                }

                if (updateToken)
                {
                    dbContext.Devices.Update(device);
                    await dbContext.SaveChangesAsync().ConfigureAwait(false);
                }
            }
            else
            {
                var key = await dbContext.ApiKeys.FirstOrDefaultAsync(apiKey => apiKey.AccessToken == token).ConfigureAwait(false);
                if (key != null)
                {
                    authInfo.IsAuthenticated = true;
                    authInfo.Client = key.Name;
                    authInfo.Token = key.AccessToken;
                    authInfo.DeviceId = string.Empty;
                    authInfo.Device = string.Empty;
                    authInfo.Version = string.Empty;
                }
            }

            return authInfo;
        }

        /// <summary>
        /// Gets the auth.
        /// </summary>
        /// <param name="httpReq">The HTTP req.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private static Dictionary<string, string>? GetAuthorizationDictionary(HttpContext httpReq)
        {
            var auth = httpReq.Request.Headers["X-Emby-Authorization"];

            if (string.IsNullOrEmpty(auth))
            {
                auth = httpReq.Request.Headers[HeaderNames.Authorization];
            }

            return auth.Count > 0 ? GetAuthorization(auth[0]) : null;
        }

        /// <summary>
        /// Gets the auth.
        /// </summary>
        /// <param name="httpReq">The HTTP req.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private static Dictionary<string, string>? GetAuthorizationDictionary(HttpRequest httpReq)
        {
            var auth = httpReq.Headers["X-Emby-Authorization"];

            if (string.IsNullOrEmpty(auth))
            {
                auth = httpReq.Headers[HeaderNames.Authorization];
            }

            return auth.Count > 0 ? GetAuthorization(auth[0]) : null;
        }

        /// <summary>
        /// Gets the authorization.
        /// </summary>
        /// <param name="authorizationHeader">The authorization header.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private static Dictionary<string, string>? GetAuthorization(ReadOnlySpan<char> authorizationHeader)
        {
            var firstSpace = authorizationHeader.IndexOf(' ');

            // There should be at least two parts
            if (firstSpace == -1)
            {
                return null;
            }

            var name = authorizationHeader[..firstSpace];

            if (!name.Equals("MediaBrowser", StringComparison.OrdinalIgnoreCase)
                && !name.Equals("Emby", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            authorizationHeader = authorizationHeader[(firstSpace + 1)..];

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in authorizationHeader.Split(','))
            {
                var trimmedItem = item.Trim();
                var firstEqualsSign = trimmedItem.IndexOf('=');

                if (firstEqualsSign > 0)
                {
                    var key = trimmedItem[..firstEqualsSign].ToString();
                    var value = NormalizeValue(trimmedItem[(firstEqualsSign + 1)..].Trim('"').ToString());
                    result[key] = value;
                }
            }

            return result;
        }

        private static string NormalizeValue(string value)
        {
            return string.IsNullOrEmpty(value) ? value : WebUtility.HtmlEncode(value);
        }
    }
}

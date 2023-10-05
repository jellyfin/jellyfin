#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Server.Implementations.Security
{
    public class AuthorizationContext : IAuthorizationContext
    {
        private readonly IDbContextFactory<JellyfinDbContext> _jellyfinDbProvider;
        private readonly IUserManager _userManager;
        private readonly IServerApplicationHost _serverApplicationHost;

        public AuthorizationContext(
            IDbContextFactory<JellyfinDbContext> jellyfinDb,
            IUserManager userManager,
            IServerApplicationHost serverApplicationHost)
        {
            _jellyfinDbProvider = jellyfinDb;
            _userManager = userManager;
            _serverApplicationHost = serverApplicationHost;
        }

        public Task<AuthorizationInfo> GetAuthorizationInfo(HttpContext requestContext)
        {
            if (requestContext.Request.HttpContext.Items.TryGetValue("AuthorizationInfo", out var cached) && cached is not null)
            {
                return Task.FromResult((AuthorizationInfo)cached); // Cache should never contain null
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
        /// <param name="httpContext">The HTTP context.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private async Task<AuthorizationInfo> GetAuthorization(HttpContext httpContext)
        {
            var authInfo = await GetAuthorizationInfo(httpContext.Request).ConfigureAwait(false);

            httpContext.Request.HttpContext.Items["AuthorizationInfo"] = authInfo;
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

            if (auth is not null)
            {
                auth.TryGetValue("DeviceId", out deviceId);
                auth.TryGetValue("Device", out deviceName);
                auth.TryGetValue("Client", out client);
                auth.TryGetValue("Version", out version);
                auth.TryGetValue("Token", out token);
            }

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

            authInfo.HasToken = true;
            var dbContext = await _jellyfinDbProvider.CreateDbContextAsync().ConfigureAwait(false);
            await using (dbContext.ConfigureAwait(false))
            {
                var device = await dbContext.Devices.FirstOrDefaultAsync(d => d.AccessToken == token).ConfigureAwait(false);

                if (device is not null)
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

                    authInfo.User = _userManager.GetUserById(device.UserId);

                    if (updateToken)
                    {
                        dbContext.Devices.Update(device);
                        await dbContext.SaveChangesAsync().ConfigureAwait(false);
                    }
                }
                else
                {
                    var key = await dbContext.ApiKeys.FirstOrDefaultAsync(apiKey => apiKey.AccessToken == token).ConfigureAwait(false);
                    if (key is not null)
                    {
                        authInfo.IsAuthenticated = true;
                        authInfo.Client = key.Name;
                        authInfo.Token = key.AccessToken;
                        if (string.IsNullOrWhiteSpace(authInfo.DeviceId))
                        {
                            authInfo.DeviceId = _serverApplicationHost.SystemId;
                        }

                        if (string.IsNullOrWhiteSpace(authInfo.Device))
                        {
                            authInfo.Device = _serverApplicationHost.Name;
                        }

                        if (string.IsNullOrWhiteSpace(authInfo.Version))
                        {
                            authInfo.Version = _serverApplicationHost.ApplicationVersionString;
                        }

                        authInfo.IsApiKey = true;
                    }
                }

                return authInfo;
            }
        }

        /// <summary>
        /// Gets the auth.
        /// </summary>
        /// <param name="httpReq">The HTTP request.</param>
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

            // Remove up until the first space
            authorizationHeader = authorizationHeader[(firstSpace + 1)..];
            return GetParts(authorizationHeader);
        }

        /// <summary>
        /// Get the authorization header components.
        /// </summary>
        /// <param name="authorizationHeader">The authorization header.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        public static Dictionary<string, string> GetParts(ReadOnlySpan<char> authorizationHeader)
        {
            var result = new Dictionary<string, string>();
            var escaped = false;
            int start = 0;
            string key = string.Empty;

            int i;
            for (i = 0; i < authorizationHeader.Length; i++)
            {
                var token = authorizationHeader[i];
                if (token == '"' || token == ',')
                {
                    // Applying a XOR logic to evaluate whether it is opening or closing a value
                    escaped = (!escaped) == (token == '"');
                    if (token == ',' && !escaped)
                    {
                        // Meeting a comma after a closing escape char means the value is complete
                        if (start < i)
                        {
                            result[key] = WebUtility.UrlDecode(authorizationHeader[start..i].Trim('"').ToString());
                            key = string.Empty;
                        }

                        start = i + 1;
                    }
                }
                else if (!escaped && token == '=')
                {
                    key = authorizationHeader[start.. i].Trim().ToString();
                    start = i + 1;
                }
            }

            // Add last value
            if (start < i)
            {
                result[key] = WebUtility.UrlDecode(authorizationHeader[start..i].Trim('"').ToString());
            }

            return result;
        }
    }
}

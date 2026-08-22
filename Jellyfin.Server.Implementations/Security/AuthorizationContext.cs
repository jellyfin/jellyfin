#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Queries;
using Jellyfin.Database.Implementations;
using Jellyfin.Extensions;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Server.Implementations.Security
{
    public class AuthorizationContext : IAuthorizationContext
    {
        /// <summary>
        /// How long a device's last activity may go stale before it is written back.
        /// </summary>
        private static readonly TimeSpan _activityUpdateInterval = TimeSpan.FromMinutes(3);

        /// <summary>
        /// Guards the read-then-write of a device's last activity, so a burst of parallel requests
        /// produces one database write instead of one per request.
        /// </summary>
        private static readonly Lock _activityLock = new();

        private readonly IDbContextFactory<JellyfinDbContext> _jellyfinDbProvider;
        private readonly IUserManager _userManager;
        private readonly IDeviceManager _deviceManager;
        private readonly IServerApplicationHost _serverApplicationHost;
        private readonly IServerConfigurationManager _configurationManager;
        private readonly ILogger<AuthorizationContext> _logger;

        public AuthorizationContext(
            IDbContextFactory<JellyfinDbContext> jellyfinDb,
            IUserManager userManager,
            IDeviceManager deviceManager,
            IServerApplicationHost serverApplicationHost,
            IServerConfigurationManager configurationManager,
            ILogger<AuthorizationContext> logger)
        {
            _jellyfinDbProvider = jellyfinDb;
            _userManager = userManager;
            _deviceManager = deviceManager;
            _serverApplicationHost = serverApplicationHost;
            _configurationManager = configurationManager;
            _logger = logger;
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
            Dictionary<string, string>? auth,
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

            if (_configurationManager.Configuration.EnableLegacyAuthorization && string.IsNullOrEmpty(token))
            {
                token = headers["X-Emby-Token"];
            }

            if (_configurationManager.Configuration.EnableLegacyAuthorization && string.IsNullOrEmpty(token))
            {
                token = headers["X-MediaBrowser-Token"];
            }

            if (string.IsNullOrEmpty(token))
            {
                token = queryString["ApiKey"];
            }

            if (_configurationManager.Configuration.EnableLegacyAuthorization && string.IsNullOrEmpty(token))
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
                IsAuthenticated = false
            };

            if (!authInfo.HasToken)
            {
                // Request doesn't contain a token.
                return authInfo;
            }

            var device = _deviceManager.GetDevices(
                new DeviceQuery { AccessToken = token }).Items.FirstOrDefault();

            if (device is not null)
            {
                authInfo.IsAuthenticated = true;
                var updateToken = false;

                // The device is the instance the manager caches, so everything below mutates shared
                // state before it is written.
                (string Previous, string Stamped)? deviceNameChange = null;
                (string Previous, string Stamped)? appVersionChange = null;
                (DateTime Previous, DateTime Stamped)? activityChange = null;

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
                        deviceNameChange = (device.DeviceName, authInfo.Device);
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
                        appVersionChange = (device.AppVersion, authInfo.Version);
                        device.AppVersion = authInfo.Version;
                    }
                }

                lock (_activityLock)
                {
                    var now = DateTime.UtcNow;
                    if (now - device.DateLastActivity > _activityUpdateInterval)
                    {
                        activityChange = (device.DateLastActivity, now);
                        device.DateLastActivity = now;
                        updateToken = true;
                    }
                }

                authInfo.User = _userManager.GetUserById(device.UserId);

                if (updateToken)
                {
                    try
                    {
                        await _deviceManager.UpdateDevice(device).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        // Every rollback is conditional: a later request may already have moved the
                        // value on, and that one owns it.
                        if (deviceNameChange is { } name
                            && string.Equals(device.DeviceName, name.Stamped, StringComparison.Ordinal))
                        {
                            device.DeviceName = name.Previous;
                        }

                        if (appVersionChange is { } appVersion
                            && string.Equals(device.AppVersion, appVersion.Stamped, StringComparison.Ordinal))
                        {
                            device.AppVersion = appVersion.Previous;
                        }

                        if (activityChange is { } activity)
                        {
                            lock (_activityLock)
                            {
                                if (device.DateLastActivity == activity.Stamped)
                                {
                                    device.DateLastActivity = activity.Previous;
                                }
                            }
                        }

                        // Refreshing the device is bookkeeping for an already authorized request, so
                        // a failed write must not turn that request into an error.
                        _logger.LogWarning(ex, "Failed to update device {DeviceId}.", device.DeviceId);
                    }
                }
            }
            else
            {
                // Only the API key branch reads from the database, so the context is created
                // here rather than for every authenticated request.
                var dbContext = await _jellyfinDbProvider.CreateDbContextAsync().ConfigureAwait(false);
                await using (dbContext.ConfigureAwait(false))
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
            }

            return authInfo;
        }

        /// <summary>
        /// Gets the auth.
        /// </summary>
        /// <param name="httpReq">The HTTP request.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private Dictionary<string, string>? GetAuthorizationDictionary(HttpRequest httpReq)
        {
            var auth = httpReq.Headers[HeaderNames.Authorization];

            if (_configurationManager.Configuration.EnableLegacyAuthorization && string.IsNullOrEmpty(auth))
            {
                auth = httpReq.Headers["X-Emby-Authorization"];
            }

            return auth.Count > 0 ? GetAuthorization(auth[0]) : null;
        }

        /// <summary>
        /// Gets the authorization.
        /// </summary>
        /// <param name="authorizationHeader">The authorization header.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private Dictionary<string, string>? GetAuthorization(ReadOnlySpan<char> authorizationHeader)
        {
            var firstSpace = authorizationHeader.IndexOf(' ');

            // There should be at least two parts
            if (firstSpace == -1)
            {
                return null;
            }

            var name = authorizationHeader[..firstSpace];

            var validName = name.Equals("MediaBrowser", StringComparison.OrdinalIgnoreCase);
            validName = validName || (_configurationManager.Configuration.EnableLegacyAuthorization && name.Equals("Emby", StringComparison.OrdinalIgnoreCase));

            if (!validName)
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
                    key = authorizationHeader[start..i].Trim().ToString();
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

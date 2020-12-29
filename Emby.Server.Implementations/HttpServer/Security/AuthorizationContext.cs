#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace Emby.Server.Implementations.HttpServer.Security
{
    public class AuthorizationContext : IAuthorizationContext
    {
        private readonly IAuthenticationRepository _authRepo;
        private readonly IUserManager _userManager;

        public AuthorizationContext(IAuthenticationRepository authRepo, IUserManager userManager)
        {
            _authRepo = authRepo;
            _userManager = userManager;
        }

        public AuthorizationInfo GetAuthorizationInfo(HttpContext requestContext)
        {
            if (requestContext.Request.HttpContext.Items.TryGetValue("AuthorizationInfo", out var cached))
            {
                return (AuthorizationInfo)cached;
            }

            return GetAuthorization(requestContext);
        }

        public AuthorizationInfo GetAuthorizationInfo(HttpRequest requestContext)
        {
            var auth = GetAuthorizationDictionary(requestContext);
            var authInfo = GetAuthorizationInfoFromDictionary(auth, requestContext.Headers, requestContext.Query);
            return authInfo;
        }

        /// <summary>
        /// Gets the authorization.
        /// </summary>
        /// <param name="httpReq">The HTTP req.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private AuthorizationInfo GetAuthorization(HttpContext httpReq)
        {
            var auth = GetAuthorizationDictionary(httpReq);
            var authInfo = GetAuthorizationInfoFromDictionary(auth, httpReq.Request.Headers, httpReq.Request.Query);

            httpReq.Request.HttpContext.Items["AuthorizationInfo"] = authInfo;
            return authInfo;
        }

        private AuthorizationInfo GetAuthorizationInfoFromDictionary(
            in Dictionary<string, string> auth,
            in IHeaderDictionary headers,
            in IQueryCollection queryString)
        {
            string deviceId = null;
            string device = null;
            string client = null;
            string version = null;
            string token = null;

            if (auth != null)
            {
                auth.TryGetValue("DeviceId", out deviceId);
                auth.TryGetValue("Device", out device);
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
                Device = device,
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
            var result = _authRepo.Get(new AuthenticationInfoQuery
            {
                AccessToken = token
            });

            if (result.Items.Count > 0)
            {
                authInfo.IsAuthenticated = true;
            }

            var originalAuthenticationInfo = result.Items.Count > 0 ? result.Items[0] : null;

            if (originalAuthenticationInfo != null)
            {
                var updateToken = false;

                // TODO: Remove these checks for IsNullOrWhiteSpace
                if (string.IsNullOrWhiteSpace(authInfo.Client))
                {
                    authInfo.Client = originalAuthenticationInfo.AppName;
                }

                if (string.IsNullOrWhiteSpace(authInfo.DeviceId))
                {
                    authInfo.DeviceId = originalAuthenticationInfo.DeviceId;
                }

                // Temporary. TODO - allow clients to specify that the token has been shared with a casting device
                var allowTokenInfoUpdate = authInfo.Client == null || authInfo.Client.IndexOf("chromecast", StringComparison.OrdinalIgnoreCase) == -1;

                if (string.IsNullOrWhiteSpace(authInfo.Device))
                {
                    authInfo.Device = originalAuthenticationInfo.DeviceName;
                }
                else if (!string.Equals(authInfo.Device, originalAuthenticationInfo.DeviceName, StringComparison.OrdinalIgnoreCase))
                {
                    if (allowTokenInfoUpdate)
                    {
                        updateToken = true;
                        originalAuthenticationInfo.DeviceName = authInfo.Device;
                    }
                }

                if (string.IsNullOrWhiteSpace(authInfo.Version))
                {
                    authInfo.Version = originalAuthenticationInfo.AppVersion;
                }
                else if (!string.Equals(authInfo.Version, originalAuthenticationInfo.AppVersion, StringComparison.OrdinalIgnoreCase))
                {
                    if (allowTokenInfoUpdate)
                    {
                        updateToken = true;
                        originalAuthenticationInfo.AppVersion = authInfo.Version;
                    }
                }

                if ((DateTime.UtcNow - originalAuthenticationInfo.DateLastActivity).TotalMinutes > 3)
                {
                    originalAuthenticationInfo.DateLastActivity = DateTime.UtcNow;
                    updateToken = true;
                }

                if (!originalAuthenticationInfo.UserId.Equals(Guid.Empty))
                {
                    authInfo.User = _userManager.GetUserById(originalAuthenticationInfo.UserId);

                    if (authInfo.User != null && !string.Equals(authInfo.User.Username, originalAuthenticationInfo.UserName, StringComparison.OrdinalIgnoreCase))
                    {
                        originalAuthenticationInfo.UserName = authInfo.User.Username;
                        updateToken = true;
                    }

                    authInfo.IsApiKey = false;
                }
                else
                {
                    authInfo.IsApiKey = true;
                }

                if (updateToken)
                {
                    _authRepo.Update(originalAuthenticationInfo);
                }
            }

            return authInfo;
        }

        /// <summary>
        /// Gets the auth.
        /// </summary>
        /// <param name="httpReq">The HTTP req.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private Dictionary<string, string> GetAuthorizationDictionary(HttpContext httpReq)
        {
            var auth = httpReq.Request.Headers["X-Emby-Authorization"];

            if (string.IsNullOrEmpty(auth))
            {
                auth = httpReq.Request.Headers[HeaderNames.Authorization];
            }

            return GetAuthorization(auth);
        }

        /// <summary>
        /// Gets the auth.
        /// </summary>
        /// <param name="httpReq">The HTTP req.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private Dictionary<string, string> GetAuthorizationDictionary(HttpRequest httpReq)
        {
            var auth = httpReq.Headers["X-Emby-Authorization"];

            if (string.IsNullOrEmpty(auth))
            {
                auth = httpReq.Headers[HeaderNames.Authorization];
            }

            return GetAuthorization(auth);
        }

        /// <summary>
        /// Gets the authorization.
        /// </summary>
        /// <param name="authorizationHeader">The authorization header.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private Dictionary<string, string> GetAuthorization(string authorizationHeader)
        {
            if (authorizationHeader == null)
            {
                return null;
            }

            var parts = authorizationHeader.Split(' ', 2);

            // There should be at least to parts
            if (parts.Length != 2)
            {
                return null;
            }

            var acceptedNames = new[] { "MediaBrowser", "Emby" };

            // It has to be a digest request
            if (!acceptedNames.Contains(parts[0], StringComparer.OrdinalIgnoreCase))
            {
                return null;
            }

            // Remove uptil the first space
            authorizationHeader = parts[1];
            parts = authorizationHeader.Split(',');

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in parts)
            {
                var param = item.Trim().Split('=', 2);

                if (param.Length == 2)
                {
                    var value = NormalizeValue(param[1].Trim('"'));
                    result[param[0]] = value;
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

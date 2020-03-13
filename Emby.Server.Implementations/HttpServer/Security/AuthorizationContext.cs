#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using MediaBrowser.Model.Services;
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

        public AuthorizationInfo GetAuthorizationInfo(object requestContext)
        {
            return GetAuthorizationInfo((IRequest)requestContext);
        }

        public AuthorizationInfo GetAuthorizationInfo(IRequest requestContext)
        {
            if (requestContext.Items.TryGetValue("AuthorizationInfo", out var cached))
            {
                return (AuthorizationInfo)cached;
            }

            return GetAuthorization(requestContext);
        }

        /// <summary>
        /// Gets the authorization.
        /// </summary>
        /// <param name="httpReq">The HTTP req.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private AuthorizationInfo GetAuthorization(IRequest httpReq)
        {
            var auth = GetAuthorizationDictionary(httpReq);

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
                token = httpReq.Headers["X-Emby-Token"];
            }

            if (string.IsNullOrEmpty(token))
            {
                token = httpReq.Headers["X-MediaBrowser-Token"];
            }
            if (string.IsNullOrEmpty(token))
            {
                token = httpReq.QueryString["api_key"];
            }

            var info = new AuthorizationInfo
            {
                Client = client,
                Device = device,
                DeviceId = deviceId,
                Version = version,
                Token = token
            };

            if (!string.IsNullOrWhiteSpace(token))
            {
                var result = _authRepo.Get(new AuthenticationInfoQuery
                {
                    AccessToken = token
                });

                var tokenInfo = result.Items.Count > 0 ? result.Items[0] : null;

                if (tokenInfo != null)
                {
                    var updateToken = false;

                    // TODO: Remove these checks for IsNullOrWhiteSpace
                    if (string.IsNullOrWhiteSpace(info.Client))
                    {
                        info.Client = tokenInfo.AppName;
                    }

                    if (string.IsNullOrWhiteSpace(info.DeviceId))
                    {
                        info.DeviceId = tokenInfo.DeviceId;
                    }

                    // Temporary. TODO - allow clients to specify that the token has been shared with a casting device
                    var allowTokenInfoUpdate = info.Client == null || info.Client.IndexOf("chromecast", StringComparison.OrdinalIgnoreCase) == -1;

                    if (string.IsNullOrWhiteSpace(info.Device))
                    {
                        info.Device = tokenInfo.DeviceName;
                    }

                    else if (!string.Equals(info.Device, tokenInfo.DeviceName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (allowTokenInfoUpdate)
                        {
                            updateToken = true;
                            tokenInfo.DeviceName = info.Device;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(info.Version))
                    {
                        info.Version = tokenInfo.AppVersion;
                    }
                    else if (!string.Equals(info.Version, tokenInfo.AppVersion, StringComparison.OrdinalIgnoreCase))
                    {
                        if (allowTokenInfoUpdate)
                        {
                            updateToken = true;
                            tokenInfo.AppVersion = info.Version;
                        }
                    }

                    if ((DateTime.UtcNow - tokenInfo.DateLastActivity).TotalMinutes > 3)
                    {
                        tokenInfo.DateLastActivity = DateTime.UtcNow;
                        updateToken = true;
                    }

                    if (!tokenInfo.UserId.Equals(Guid.Empty))
                    {
                        info.User = _userManager.GetUserById(tokenInfo.UserId);

                        if (info.User != null && !string.Equals(info.User.Name, tokenInfo.UserName, StringComparison.OrdinalIgnoreCase))
                        {
                            tokenInfo.UserName = info.User.Name;
                            updateToken = true;
                        }
                    }

                    if (updateToken)
                    {
                        _authRepo.Update(tokenInfo);
                    }
                }
                httpReq.Items["OriginalAuthenticationInfo"] = tokenInfo;
            }

            httpReq.Items["AuthorizationInfo"] = info;

            return info;
        }

        /// <summary>
        /// Gets the auth.
        /// </summary>
        /// <param name="httpReq">The HTTP req.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private Dictionary<string, string> GetAuthorizationDictionary(IRequest httpReq)
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

            var parts = authorizationHeader.Split(new[] { ' ' }, 2);

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
                var param = item.Trim().Split(new[] { '=' }, 2);

                if (param.Length == 2)
                {
                    var value = NormalizeValue(param[1].Trim(new[] { '"' }));
                    result.Add(param[0], value);
                }
            }

            return result;
        }

        private static string NormalizeValue(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return WebUtility.HtmlEncode(value);
        }
    }
}

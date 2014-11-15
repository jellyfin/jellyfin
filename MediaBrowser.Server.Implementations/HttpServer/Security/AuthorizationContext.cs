using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using ServiceStack.Web;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Server.Implementations.HttpServer.Security
{
    public class AuthorizationContext : IAuthorizationContext
    {
        private readonly IAuthenticationRepository _authRepo;

        public AuthorizationContext(IAuthenticationRepository authRepo)
        {
            _authRepo = authRepo;
        }

        public AuthorizationInfo GetAuthorizationInfo(object requestContext)
        {
            var req = new ServiceStackServiceRequest((IRequest)requestContext);
            return GetAuthorizationInfo(req);
        }

        public AuthorizationInfo GetAuthorizationInfo(IServiceRequest requestContext)
        {
            object cached;
            if (requestContext.Items.TryGetValue("AuthorizationInfo", out cached))
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
        private AuthorizationInfo GetAuthorization(IServiceRequest httpReq)
        {
            var auth = GetAuthorizationDictionary(httpReq);

            string userId = null;
            string deviceId = null;
            string device = null;
            string client = null;
            string version = null;

            if (auth != null)
            {
                // TODO: Remove this 
                auth.TryGetValue("UserId", out userId);

                auth.TryGetValue("DeviceId", out deviceId);
                auth.TryGetValue("Device", out device);
                auth.TryGetValue("Client", out client);
                auth.TryGetValue("Version", out version);
            }

            var token = httpReq.Headers["X-MediaBrowser-Token"];

            if (string.IsNullOrWhiteSpace(token))
            {
                token = httpReq.QueryString["api_key"];
            }

            // Hack until iOS is updated
            // TODO: Remove
            if (string.IsNullOrWhiteSpace(client))
            {
                var userAgent = httpReq.Headers["User-Agent"] ?? string.Empty;

                if (userAgent.IndexOf("mediabrowserios", StringComparison.OrdinalIgnoreCase) != -1 ||
                    userAgent.IndexOf("iphone", StringComparison.OrdinalIgnoreCase) != -1 ||
                    userAgent.IndexOf("ipad", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    client = "iOS";
                }

                else if (userAgent.IndexOf("crKey", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    client = "Chromecast";
                }
            }

            // Hack until iOS is updated
            // TODO: Remove
            if (string.IsNullOrWhiteSpace(device))
            {
                var userAgent = httpReq.Headers["User-Agent"] ?? string.Empty;

                if (userAgent.IndexOf("iPhone", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    device = "iPhone";
                }

                else if (userAgent.IndexOf("iPad", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    device = "iPad";
                }

                else if (userAgent.IndexOf("crKey", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    device = "Chromecast";
                }
            }

            var info = new AuthorizationInfo
            {
                Client = client,
                Device = device,
                DeviceId = deviceId,
                UserId = userId,
                Version = version,
                Token = token
            };

            if (!string.IsNullOrWhiteSpace(token))
            {
                var result = _authRepo.Get(new AuthenticationInfoQuery
                {
                    AccessToken = token
                });

                var tokenInfo = result.Items.FirstOrDefault();

                if (tokenInfo != null)
                {
                    info.UserId = tokenInfo.UserId;
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
        private Dictionary<string, string> GetAuthorizationDictionary(IServiceRequest httpReq)
        {
            var auth = httpReq.Headers["Authorization"];

            return GetAuthorization(auth);
        }

        /// <summary>
        /// Gets the authorization.
        /// </summary>
        /// <param name="authorizationHeader">The authorization header.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private Dictionary<string, string> GetAuthorization(string authorizationHeader)
        {
            if (authorizationHeader == null) return null;

            var parts = authorizationHeader.Split(new[] { ' ' }, 2);

            // There should be at least to parts
            if (parts.Length != 2) return null;

            // It has to be a digest request
            if (!string.Equals(parts[0], "MediaBrowser", StringComparison.OrdinalIgnoreCase))
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
                    result.Add(param[0], param[1].Trim(new[] { '"' }));
                }
            }

            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using MediaBrowser.Controller.Net;
using ServiceStack.Web;

namespace MediaBrowser.Server.Implementations.HttpServer.Security
{
    public class AuthorizationContext : IAuthorizationContext
    {
        public AuthorizationInfo GetAuthorizationInfo(IRequest requestContext)
        {
            return GetAuthorization(requestContext);
        }

        /// <summary>
        /// Gets the authorization.
        /// </summary>
        /// <param name="httpReq">The HTTP req.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private static AuthorizationInfo GetAuthorization(IRequest httpReq)
        {
            var auth = GetAuthorizationDictionary(httpReq);

            string userId = null;
            string deviceId = null;
            string device = null;
            string client = null;
            string version = null;

            if (auth != null)
            {
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

            return new AuthorizationInfo
            {
                Client = client,
                Device = device,
                DeviceId = deviceId,
                UserId = userId,
                Version = version,
                Token = token
            };
        }

        /// <summary>
        /// Gets the auth.
        /// </summary>
        /// <param name="httpReq">The HTTP req.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private static Dictionary<string, string> GetAuthorizationDictionary(IRequest httpReq)
        {
            var auth = httpReq.Headers["Authorization"];

            return GetAuthorization(auth);
        }

        /// <summary>
        /// Gets the authorization.
        /// </summary>
        /// <param name="authorizationHeader">The authorization header.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        private static Dictionary<string, string> GetAuthorization(string authorizationHeader)
        {
            if (authorizationHeader == null) return null;

            var parts = authorizationHeader.Split(' ');

            // There should be at least to parts
            if (parts.Length < 2) return null;

            // It has to be a digest request
            if (!string.Equals(parts[0], "MediaBrowser", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Remove uptil the first space
            authorizationHeader = authorizationHeader.Substring(authorizationHeader.IndexOf(' '));
            parts = authorizationHeader.Split(',');

            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in parts)
            {
                var param = item.Trim().Split(new[] { '=' }, 2);
                result.Add(param[0], param[1].Trim(new[] { '"' }));
            }

            return result;
        }
    }
}

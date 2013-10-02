using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using ServiceStack.Common.Web;
using ServiceStack.ServiceHost;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Api
{
    public class AuthorizationRequestFilterAttribute : Attribute, IHasRequestFilter
    {
        //This property will be resolved by the IoC container
        /// <summary>
        /// Gets or sets the user manager.
        /// </summary>
        /// <value>The user manager.</value>
        public IUserManager UserManager { get; set; }

        public ISessionManager SessionManager { get; set; }

        /// <summary>
        /// Gets or sets the logger.
        /// </summary>
        /// <value>The logger.</value>
        public ILogger Logger { get; set; }

        /// <summary>
        /// The request filter is executed before the service.
        /// </summary>
        /// <param name="request">The http request wrapper</param>
        /// <param name="response">The http response wrapper</param>
        /// <param name="requestDto">The request DTO</param>
        public void RequestFilter(IHttpRequest request, IHttpResponse response, object requestDto)
        {
            //This code is executed before the service

            var auth = GetAuthorization(request);

            if (auth != null)
            {
                User user = null;

                if (auth.ContainsKey("UserId"))
                {
                    var userId = auth["UserId"];

                    if (!string.IsNullOrEmpty(userId))
                    {
                        user = UserManager.GetUserById(new Guid(userId));
                    }
                }

                string deviceId;
                string device;
                string client;
                string version;

                auth.TryGetValue("DeviceId", out deviceId);
                auth.TryGetValue("Device", out device);
                auth.TryGetValue("Client", out client);
                auth.TryGetValue("Version", out version);

                if (!string.IsNullOrEmpty(client) && !string.IsNullOrEmpty(deviceId) && !string.IsNullOrEmpty(device) && !string.IsNullOrEmpty(version))
                {
                    SessionManager.LogSessionActivity(client, version, deviceId, device, user);
                }
            }
        }

        /// <summary>
        /// Gets the auth.
        /// </summary>
        /// <param name="httpReq">The HTTP req.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        public static Dictionary<string, string> GetAuthorization(IHttpRequest httpReq)
        {
            var auth = httpReq.Headers[HttpHeaders.Authorization];

            return GetAuthorization(auth);
        }

        /// <summary>
        /// Gets the authorization.
        /// </summary>
        /// <param name="httpReq">The HTTP req.</param>
        /// <returns>Dictionary{System.StringSystem.String}.</returns>
        public static AuthorizationInfo GetAuthorization(IRequestContext httpReq)
        {
            var header = httpReq.GetHeader("Authorization");

            var auth = GetAuthorization(header);

            string userId;
            string deviceId;
            string device;
            string client;
            string version;

            auth.TryGetValue("UserId", out userId);
            auth.TryGetValue("DeviceId", out deviceId);
            auth.TryGetValue("Device", out device);
            auth.TryGetValue("Client", out client);
            auth.TryGetValue("Version", out version);

            return new AuthorizationInfo
            {
                Client = client,
                Device = device,
                DeviceId = deviceId,
                UserId = userId,
                Version = version
            };
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

        /// <summary>
        /// A new shallow copy of this filter is used on every request.
        /// </summary>
        /// <returns>IHasRequestFilter.</returns>
        public IHasRequestFilter Copy()
        {
            return this;
        }

        /// <summary>
        /// Order in which Request Filters are executed.
        /// &lt;0 Executed before global request filters
        /// &gt;0 Executed after global request filters
        /// </summary>
        /// <value>The priority.</value>
        public int Priority
        {
            get { return 0; }
        }
    }

    public class AuthorizationInfo
    {
        public string UserId;
        public string DeviceId;
        public string Device;
        public string Client;
        public string Version;
    }
}

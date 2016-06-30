using MediaBrowser.Controller.Connect;
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
        private readonly IConnectManager _connectManager;

        public AuthorizationContext(IAuthenticationRepository authRepo, IConnectManager connectManager)
        {
            _authRepo = authRepo;
            _connectManager = connectManager;
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

            string deviceId = null;
            string device = null;
            string client = null;
            string version = null;

            if (auth != null)
            {
                auth.TryGetValue("DeviceId", out deviceId);
                auth.TryGetValue("Device", out device);
                auth.TryGetValue("Client", out client);
                auth.TryGetValue("Version", out version);
            }

            var token = httpReq.Headers["X-Emby-Token"];

            if (string.IsNullOrWhiteSpace(token))
            {
                token = httpReq.Headers["X-MediaBrowser-Token"];
            }
            if (string.IsNullOrWhiteSpace(token))
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

                var tokenInfo = result.Items.FirstOrDefault();

                if (tokenInfo != null)
                {
                    info.UserId = tokenInfo.UserId;

                    // TODO: Remove these checks for IsNullOrWhiteSpace
                    if (string.IsNullOrWhiteSpace(info.Client))
                    {
                        info.Client = tokenInfo.AppName;
                    }
                    if (string.IsNullOrWhiteSpace(info.Device))
                    {
                        info.Device = tokenInfo.DeviceName;
                    }
                    if (string.IsNullOrWhiteSpace(info.DeviceId))
                    {
                        info.DeviceId = tokenInfo.DeviceId;
                    }
                    if (string.IsNullOrWhiteSpace(info.Version))
                    {
                        info.Version = tokenInfo.AppVersion;
                    }
                }
                else
                {
                    var user = _connectManager.GetUserFromExchangeToken(token);
                    if (user != null)
                    {
                        info.UserId = user.Id.ToString("N");
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
        private Dictionary<string, string> GetAuthorizationDictionary(IServiceRequest httpReq)
        {
            var auth = httpReq.Headers["X-Emby-Authorization"];

            if (string.IsNullOrWhiteSpace(auth))
            {
                auth = httpReq.Headers["Authorization"];
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
					var value = NormalizeValue (param[1].Trim(new[] { '"' }));
                    result.Add(param[0], value);
                }
            }

            return result;
        }

		private string NormalizeValue(string value)
		{
			if (string.IsNullOrWhiteSpace (value)) 
			{
				return value;
			}

			return System.Net.WebUtility.HtmlEncode(value);
		}
    }
}

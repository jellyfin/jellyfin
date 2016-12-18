using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using System;
using MediaBrowser.Model.Services;

namespace MediaBrowser.Controller.Net
{
    public class LoggedAttribute : IRequestFilter
    {
        public LoggedAttribute(ILogger logger, IUserManager userManager, ISessionManager sessionManager, IAuthorizationContext authorizationContext)
        {
            Logger = logger;
            UserManager = userManager;
            SessionManager = sessionManager;
            AuthorizationContext = authorizationContext;
        }

        public ILogger Logger { get; private set; }
        public IUserManager UserManager { get; private set; }
        public ISessionManager SessionManager { get; private set; }
        public IAuthorizationContext AuthorizationContext { get; private set; }

        /// <summary>
        /// The request filter is executed before the service.
        /// </summary>
        /// <param name="request">The http request wrapper</param>
        /// <param name="response">The http response wrapper</param>
        /// <param name="requestDto">The request DTO</param>
        public void Filter(IRequest request, IResponse response, object requestDto)
        {
            var serviceRequest = new ServiceRequest(request);
            
            //This code is executed before the service
            var auth = AuthorizationContext.GetAuthorizationInfo(serviceRequest);

            if (auth != null)
            {
                User user = null;

                if (!string.IsNullOrWhiteSpace(auth.UserId))
                {
                    var userId = auth.UserId;

                    user = UserManager.GetUserById(userId);
                }

                string deviceId = auth.DeviceId;
                string device = auth.Device;
                string client = auth.Client;
                string version = auth.Version;

                if (!string.IsNullOrEmpty(client) && !string.IsNullOrEmpty(deviceId) && !string.IsNullOrEmpty(device) && !string.IsNullOrEmpty(version))
                {
                    var remoteEndPoint = request.RemoteIp;

                    SessionManager.LogSessionActivity(client, version, deviceId, device, remoteEndPoint, user);
                }
            }
        }
    }
}

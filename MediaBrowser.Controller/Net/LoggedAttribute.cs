using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Logging;
using ServiceStack.Web;
using System;

namespace MediaBrowser.Controller.Net
{
    public class LoggedAttribute : Attribute, IHasRequestFilter
    {
        public ILogger Logger { get; set; }
        public IUserManager UserManager { get; set; }
        public ISessionManager SessionManager { get; set; }
        public IAuthorizationContext AuthorizationContext { get; set; }

        /// <summary>
        /// The request filter is executed before the service.
        /// </summary>
        /// <param name="request">The http request wrapper</param>
        /// <param name="response">The http response wrapper</param>
        /// <param name="requestDto">The request DTO</param>
        public void RequestFilter(IRequest request, IResponse response, object requestDto)
        {
            var serviceRequest = new ServiceStackServiceRequest(request);
            
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
}

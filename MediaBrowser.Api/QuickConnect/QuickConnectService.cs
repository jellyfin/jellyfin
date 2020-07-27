using System;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.QuickConnect;
using MediaBrowser.Model.QuickConnect;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.QuickConnect
{
    [Route("/QuickConnect/Initiate", "GET", Summary = "Requests a new quick connect code")]
    public class Initiate : IReturn<QuickConnectResult>
    {
        [ApiMember(Name = "FriendlyName", Description = "Device friendly name", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string FriendlyName { get; set; }
    }

    [Route("/QuickConnect/Connect", "GET", Summary = "Attempts to retrieve authentication information")]
    public class Connect : IReturn<QuickConnectResult>
    {
        [ApiMember(Name = "Secret", Description = "Quick connect secret", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Secret { get; set; }
    }

    [Route("/QuickConnect/Authorize", "POST", Summary = "Authorizes a pending quick connect request")]
    [Authenticated]
    public class Authorize : IReturn<bool>
    {
        [ApiMember(Name = "Code", Description = "Quick connect identifying code", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Code { get; set; }
    }

    [Route("/QuickConnect/Deauthorize", "POST", Summary = "Deletes all quick connect authorization tokens for the current user")]
    [Authenticated]
    public class Deauthorize : IReturn<int>
    {
        [ApiMember(Name = "UserId", Description = "User Id", IsRequired = false, DataType = "string", ParameterType = "query", Verb = "GET")]
        public Guid UserId { get; set; }
    }

    [Route("/QuickConnect/Status", "GET", Summary = "Gets the current quick connect state")]
    public class QuickConnectStatus : IReturn<QuickConnectResult>
    {

    }

    [Route("/QuickConnect/Available", "POST", Summary = "Enables or disables quick connect")]
    [Authenticated(Roles = "Admin")]
    public class Available : IReturn<QuickConnectState>
    {
        [ApiMember(Name = "Status", Description = "New quick connect status", IsRequired = false, DataType = "QuickConnectState", ParameterType = "query", Verb = "GET")]
        public QuickConnectState Status { get; set; }
    }

    [Route("/QuickConnect/Activate", "POST", Summary = "Temporarily activates quick connect for the time period defined in the server configuration")]
    [Authenticated]
    public class Activate : IReturn<bool>
    {

    }

    public class QuickConnectService : BaseApiService
    {
        private IQuickConnect _quickConnect;
        private IUserManager _userManager;
        private IAuthorizationContext _authContext;

        public QuickConnectService(
            ILogger<QuickConnectService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            IUserManager userManager,
            IAuthorizationContext authContext,
            IQuickConnect quickConnect)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _userManager = userManager;
            _quickConnect = quickConnect;
            _authContext = authContext;
        }

        public object Get(Initiate request)
        {
            return _quickConnect.TryConnect(request.FriendlyName);
        }

        public object Get(Connect request)
        {
            return _quickConnect.CheckRequestStatus(request.Secret);
        }

        public object Get(QuickConnectStatus request)
        {
            _quickConnect.ExpireRequests();
            return _quickConnect.State;
        }

        public object Post(Deauthorize request)
        {
            AssertCanUpdateUser(_authContext, _userManager, request.UserId, true);

            return _quickConnect.DeleteAllDevices(request.UserId);
        }

        public object Post(Authorize request)
        {
            return _quickConnect.AuthorizeRequest(Request, request.Code);
        }

        public object Post(Activate request)
        {
            if (_quickConnect.State == QuickConnectState.Unavailable)
            {
                return false;
            }

            string name = _authContext.GetAuthorizationInfo(Request).User.Username;

            Logger.LogInformation("{name} temporarily activated quick connect", name);
            _quickConnect.Activate();

            return true;
        }

        public object Post(Available request)
        {
            _quickConnect.SetState(request.Status);
            return _quickConnect.State;
        }
    }
}

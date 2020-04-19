using System;
using System.Collections.Generic;
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

    [Route("/QuickConnect/List", "GET", Summary = "Lists all quick connect requests")]
    [Authenticated]
    public class QuickConnectList : IReturn<List<QuickConnectResultDto>>
    {
    }

    [Route("/QuickConnect/Authorize", "POST", Summary = "Authorizes a pending quick connect request")]
    [Authenticated]
    public class Authorize : IReturn<QuickConnectResultDto>
    {
        [ApiMember(Name = "Lookup", Description = "Quick connect public lookup", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Lookup { get; set; }
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
    public class Activate : IReturn<QuickConnectState>
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

        public object Get(QuickConnectList request)
        {
            if(_quickConnect.State != QuickConnectState.Active)
            {
                return Array.Empty<QuickConnectResultDto>();
            }

            return _quickConnect.GetCurrentRequests();
        }

        public object Get(QuickConnectStatus request)
        {
            return _quickConnect.State;
        }

        public object Post(Deauthorize request)
        {
            AssertCanUpdateUser(_authContext, _userManager, request.UserId, true);

            return _quickConnect.DeleteAllDevices(request.UserId);
        }

        public object Post(Authorize request)
        {
            bool result = _quickConnect.AuthorizeRequest(Request, request.Lookup);

            Logger.LogInformation("Result of authorizing quick connect {0}: {1}", request.Lookup[..10], result);

            return result;
        }

        public object Post(Activate request)
        {
            string name = _authContext.GetAuthorizationInfo(Request).User.Name;

            if(_quickConnect.State == QuickConnectState.Unavailable)
            {
                return new QuickConnectResult()
                {
                    Error = "Quick connect is not enabled on this server"
                };
            }

            else if(_quickConnect.State == QuickConnectState.Available)
            {
                var result = _quickConnect.Activate();

                if (string.IsNullOrEmpty(result.Error))
                {
                    Logger.LogInformation("{name} temporarily activated quick connect", name);
                }

                return result;
            }

            else if(_quickConnect.State == QuickConnectState.Active)
            {
                return new QuickConnectResult()
                {
                    Error = ""
                };
            }

            return new QuickConnectResult()
            {
                Error = "Unknown current state"
            };
        }

        public object Post(Available request)
        {
            _quickConnect.SetEnabled(request.Status);

            return _quickConnect.State;
        }
    }
}

using System;
using System.Globalization;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;

namespace MediaBrowser.Api.Sessions
{
    [Route("/Auth/Keys", "GET")]
    [Authenticated(Roles = "Admin")]
    public class GetKeys
    {
    }

    [Route("/Auth/Keys/{Key}", "DELETE")]
    [Authenticated(Roles = "Admin")]
    public class RevokeKey
    {
        [ApiMember(Name = "Key", Description = "Authentication key", IsRequired = true, DataType = "string", ParameterType = "path", Verb = "DELETE")]
        public string Key { get; set; }
    }

    [Route("/Auth/Keys", "POST")]
    [Authenticated(Roles = "Admin")]
    public class CreateKey
    {
        [ApiMember(Name = "App", Description = "Name of the app using the authentication key", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string App { get; set; }
    }

    public class ApiKeyService : BaseApiService
    {
        private readonly ISessionManager _sessionManager;

        private readonly IAuthenticationRepository _authRepo;

        private readonly IServerApplicationHost _appHost;

        public ApiKeyService(
            ILogger<ApiKeyService> logger,
            IServerConfigurationManager serverConfigurationManager,
            IHttpResultFactory httpResultFactory,
            ISessionManager sessionManager,
            IServerApplicationHost appHost,
            IAuthenticationRepository authRepo)
            : base(logger, serverConfigurationManager, httpResultFactory)
        {
            _sessionManager = sessionManager;
            _authRepo = authRepo;
            _appHost = appHost;
        }

        public void Delete(RevokeKey request)
        {
            _sessionManager.RevokeToken(request.Key);
        }

        public void Post(CreateKey request)
        {
            _authRepo.Create(new AuthenticationInfo
            {
                AppName = request.App,
                AccessToken = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture),
                DateCreated = DateTime.UtcNow,
                DeviceId = _appHost.SystemId,
                DeviceName = _appHost.FriendlyName,
                AppVersion = _appHost.ApplicationVersionString
            });
        }

        public object Get(GetKeys request)
        {
            var result = _authRepo.Get(new AuthenticationInfoQuery
            {
                HasUser = false
            });

            return result;
        }
    }
}

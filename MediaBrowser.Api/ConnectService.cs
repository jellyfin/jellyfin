using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Connect;
using ServiceStack;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Api
{
    [Route("/Users/{Id}/Connect/Link", "POST", Summary = "Creates a Connect link for a user")]
    public class CreateConnectLink : IReturn<UserLinkResult>
    {
        [ApiMember(Name = "Id", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string Id { get; set; }

        [ApiMember(Name = "ConnectUsername", Description = "Connect username", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string ConnectUsername { get; set; }
    }

    [Route("/Users/{Id}/Connect/Link", "DELETE", Summary = "Removes a Connect link for a user")]
    public class DeleteConnectLink : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Route("/Connect/Invite", "POST", Summary = "Creates a Connect link for a user")]
    public class CreateConnectInvite : IReturn<UserLinkResult>
    {
        [ApiMember(Name = "ConnectUsername", Description = "Connect username", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "POST")]
        public string ConnectUsername { get; set; }

        [ApiMember(Name = "SendingUserId", Description = "Sending User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string SendingUserId { get; set; }
    }


    [Route("/Connect/Pending", "GET", Summary = "Creates a Connect link for a user")]
    public class GetPendingGuests : IReturn<List<ConnectAuthorization>>
    {
    }


    [Route("/Connect/Pending", "DELETE", Summary = "Deletes a Connect link for a user")]
    public class DeleteAuthorization : IReturnVoid
    {
        [ApiMember(Name = "Id", Description = "Authorization Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "DELETE")]
        public string Id { get; set; }
    }

    [Authenticated(Roles = "Admin")]
    public class ConnectService : BaseApiService
    {
        private readonly IConnectManager _connectManager;

        public ConnectService(IConnectManager connectManager)
        {
            _connectManager = connectManager;
        }

        public object Post(CreateConnectLink request)
        {
            return _connectManager.LinkUser(request.Id, request.ConnectUsername);
        }

        public object Post(CreateConnectInvite request)
        {
            return _connectManager.InviteUser(request.SendingUserId, request.ConnectUsername);
        }

        public void Delete(DeleteConnectLink request)
        {
            var task = _connectManager.RemoveLink(request.Id);

            Task.WaitAll(task);
        }

        public async Task<object> Get(GetPendingGuests request)
        {
            var result = await _connectManager.GetPendingGuests().ConfigureAwait(false);

            return ToOptimizedResult(result);
        }

        public void Delete(DeleteAuthorization request)
        {
            var task = _connectManager.CancelAuthorization(request.Id);

            Task.WaitAll(task);
        }
    }
}

using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Net;
using ServiceStack;
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

        public void Delete(DeleteConnectLink request)
        {
            var task = _connectManager.RemoveLink(request.Id);

            Task.WaitAll(task);
        }
    }
}

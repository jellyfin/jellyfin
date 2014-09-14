using System.Threading.Tasks;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Net;
using ServiceStack;

namespace MediaBrowser.Api
{
    [Route("/Users/{Id}/Connect/Info", "GET", Summary = "Gets connect info for a user")]
    public class GetConnectUserInfo : IReturn<ConnectUserLink>
    {
        [ApiMember(Name = "Id", Description = "User Id", IsRequired = true, DataType = "string", ParameterType = "query", Verb = "GET")]
        public string Id { get; set; }
    }

    [Route("/Users/{Id}/Connect/Link", "POST", Summary = "Creates a Connect link for a user")]
    public class CreateConnectLink : IReturn<ConnectUserLink>
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
    
    [Authenticated]
    public class ConnectService : BaseApiService
    {
        private readonly IConnectManager _connectManager;

        public ConnectService(IConnectManager connectManager)
        {
            _connectManager = connectManager;
        }

        public object Get(GetConnectUserInfo request)
        {
            var result = _connectManager.GetUserInfo(request.Id);

            return ToOptimizedResult(result);
        }

        public void Post(CreateConnectLink request)
        {
            var task = _connectManager.LinkUser(request.Id, request.ConnectUsername);

            Task.WaitAll(task);
        }

        public void Delete(DeleteConnectLink request)
        {
            var task = _connectManager.RemoveLink(request.Id);

            Task.WaitAll(task);
        }
    }
}

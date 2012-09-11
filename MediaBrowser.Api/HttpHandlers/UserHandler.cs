using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.DTO;
using System.ComponentModel.Composition;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    [Export(typeof(BaseHandler))]
    class UserHandler : BaseSerializationHandler<DTOUser>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("user", request);
        }
        
        protected override Task<DTOUser> GetObjectToSerialize()
        {
            string id = QueryString["id"];

            User user = string.IsNullOrEmpty(id) ? ApiService.GetDefaultUser(false) : ApiService.GetUserById(id, false); ;

            DTOUser dto = ApiService.GetDTOUser(user);

            return Task.FromResult<DTOUser>(dto);
        }
    }
}

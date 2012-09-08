using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    [Export(typeof(BaseHandler))]
    class UsersHandler : BaseSerializationHandler<IEnumerable<DTOUser>>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("users", request);
        }
        
        protected override Task<IEnumerable<DTOUser>> GetObjectToSerialize()
        {
            return Task.FromResult<IEnumerable<DTOUser>>(Kernel.Instance.Users.Select(u => ApiService.GetDTOUser(u)));
        }
    }
}

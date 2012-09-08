using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    class UserHandler : BaseSerializationHandler<DTOUser>
    {
        protected override Task<DTOUser> GetObjectToSerialize()
        {
            string id = QueryString["id"];

            User user = string.IsNullOrEmpty(id) ? ApiService.GetDefaultUser(false) : ApiService.GetUserById(id, false); ;

            DTOUser dto = ApiService.GetDTOUser(user);

            return Task.FromResult<DTOUser>(dto);
        }
    }
}

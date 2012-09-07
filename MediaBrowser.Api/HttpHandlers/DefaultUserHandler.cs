using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    class DefaultUserHandler : BaseSerializationHandler<DTOUser>
    {
        protected override Task<DTOUser> GetObjectToSerialize()
        {
            User user = ApiService.GetDefaultUser(false);

            DTOUser dto = ApiService.GetDTOUser(user);

            return Task.FromResult<DTOUser>(dto);
        }
    }
}

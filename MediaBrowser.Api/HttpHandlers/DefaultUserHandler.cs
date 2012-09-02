using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    class DefaultUserHandler : BaseSerializationHandler<DTOUser>
    {
        protected override Task<DTOUser> GetObjectToSerialize()
        {
            User user = Kernel.Instance.Users.FirstOrDefault();

            DTOUser dto = ApiService.GetDTOUser(user);

            return Task.FromResult<DTOUser>(dto);
        }
    }
}

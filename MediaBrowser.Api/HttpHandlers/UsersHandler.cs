using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;

namespace MediaBrowser.Api.HttpHandlers
{
    class UsersHandler : BaseSerializationHandler<IEnumerable<DTOUser>>
    {
        protected override Task<IEnumerable<DTOUser>> GetObjectToSerialize()
        {
            return Task.FromResult<IEnumerable<DTOUser>>(Kernel.Instance.Users.Select(u => ApiService.GetDTOUser(u)));
        }
    }
}

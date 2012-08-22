using System.Collections.Generic;
using System.Threading.Tasks;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    class UsersHandler : BaseJsonHandler<IEnumerable<User>>
    {
        protected override Task<IEnumerable<User>> GetObjectToSerialize()
        {
            return Task.FromResult<IEnumerable<User>>(Kernel.Instance.Users);
        }
    }
}

using System.Collections.Generic;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    class UsersHandler : BaseJsonHandler<IEnumerable<User>>
    {
        protected override IEnumerable<User> GetObjectToSerialize()
        {
            return Kernel.Instance.Users;
        }
    }
}

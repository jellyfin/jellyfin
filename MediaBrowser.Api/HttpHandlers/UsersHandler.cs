using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;

namespace MediaBrowser.Api.HttpHandlers
{
    class UsersHandler : BaseJsonHandler
    {
        protected override object GetObjectToSerialize()
        {
            return Kernel.Instance.Users;
        }
    }
}

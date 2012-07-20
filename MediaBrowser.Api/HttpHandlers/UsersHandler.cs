using MediaBrowser.Net.Handlers;
using MediaBrowser.Controller;

namespace MediaBrowser.Api.HttpHandlers
{
    class UsersHandler : JsonHandler
    {
        protected override object ObjectToSerialize
        {
            get
            {
                return Kernel.Instance.Users;
            }
        }
    }
}

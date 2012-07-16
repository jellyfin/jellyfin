using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;

namespace MediaBrowser.Api.HttpHandlers
{
    class UsersHandler : JsonHandler
    {
        public UsersHandler(RequestContext ctx)
            : base(ctx)
        {
        }

        protected override object ObjectToSerialize
        {
            get
            {
                return Kernel.Instance.Users;
            }
        }
    }
}

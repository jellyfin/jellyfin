using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Authentication;
using System.ComponentModel.Composition;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    [Export(typeof(BaseHandler))]
    class UserAuthenticationHandler : BaseSerializationHandler<AuthenticationResult>
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return ApiService.IsApiUrlMatch("UserAuthentication", request);
        }
        
        protected override async Task<AuthenticationResult> GetObjectToSerialize()
        {
            string userId = await GetFormValue("userid").ConfigureAwait(false);
            User user = ApiService.GetUserById(userId, false);

            string password = await GetFormValue("password").ConfigureAwait(false);

            return Kernel.Instance.AuthenticateUser(user, password);
        }
    }
}

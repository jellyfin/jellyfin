using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.Entities;
using System.Threading.Tasks;

namespace MediaBrowser.Api.HttpHandlers
{
    class UserAuthenticationHandler : BaseSerializationHandler<AuthenticationResult>
    {
        protected override async Task<AuthenticationResult> GetObjectToSerialize()
        {
            string userId = await GetFormValue("userid").ConfigureAwait(false);
            User user = ApiService.GetUserById(userId, false);

            string password = await GetFormValue("password").ConfigureAwait(false);

            return Kernel.Instance.AuthenticateUser(user, password);
        }
    }
}

using System;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Model.DTO;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Api.HttpHandlers
{
    class UserAuthenticationHandler : BaseSerializationHandler<AuthenticationResult>
    {
        protected override async Task<AuthenticationResult> GetObjectToSerialize()
        {
            Guid userId = Guid.Parse(await GetFormValue("userid").ConfigureAwait(false));
            User user = Kernel.Instance.Users.First(u => u.Id == userId);

            string password = await GetFormValue("password").ConfigureAwait(false);

            return new AuthenticationResult()
            {
                Success = true
            }; 
        }
    }
}

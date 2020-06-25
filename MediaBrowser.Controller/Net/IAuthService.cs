#nullable enable

using Jellyfin.Data.Entities;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller.Net
{
    public interface IAuthService
    {
        void Authenticate(IRequest request, IAuthenticationAttributes authAttribtues);

        User? Authenticate(HttpRequest request, IAuthenticationAttributes authAttribtues);

        /// <summary>
        /// Authenticate request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Authorization information. Null if unauthenticated.</returns>
        AuthorizationInfo Authenticate(HttpRequest request);
    }
}

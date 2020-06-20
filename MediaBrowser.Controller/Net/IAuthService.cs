#nullable enable

using Jellyfin.Data.Entities;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// IAuthService.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Authenticate and authorize request.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="authAttribtutes">Authorization attributes.</param>
        void Authenticate(IRequest request, IAuthenticationAttributes authAttribtutes);

        /// <summary>
        /// Authenticate and authorize request.
        /// </summary>
        /// <param name="request">Request.</param>
        /// <param name="authAttribtutes">Authorization attributes.</param>
        /// <returns>Authenticated user.</returns>
        User? Authenticate(HttpRequest request, IAuthenticationAttributes authAttribtutes);

        /// <summary>
        /// Authenticate request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Authorization information. Null if unauthenticated.</returns>
        AuthorizationInfo Authenticate(HttpRequest request);
    }
}

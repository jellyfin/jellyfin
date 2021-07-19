using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// IAuthService.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Authenticate request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns>Authorization information. Null if unauthenticated.</returns>
        Task<AuthorizationInfo> Authenticate(HttpRequest request);
    }
}

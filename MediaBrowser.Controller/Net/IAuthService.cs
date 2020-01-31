#nullable enable

using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;

namespace MediaBrowser.Controller.Net
{
    public interface IAuthService
    {
        void Authenticate(IRequest request, IAuthenticationAttributes authAttribtues);
        User? Authenticate(HttpRequest request, IAuthenticationAttributes authAttribtues);
    }
}

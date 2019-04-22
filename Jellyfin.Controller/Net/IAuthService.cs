using Jellyfin.Model.Services;

namespace Jellyfin.Controller.Net
{
    public interface IAuthService
    {
        void Authenticate(IRequest request, IAuthenticationAttributes authAttribtues);
    }
}

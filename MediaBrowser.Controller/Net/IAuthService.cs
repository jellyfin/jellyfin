using MediaBrowser.Model.Services;

namespace MediaBrowser.Controller.Net
{
    public interface IAuthService
    {
        void Authenticate(IRequest request, IAuthenticationAttributes authAttribtues);
    }
}

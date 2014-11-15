
namespace MediaBrowser.Controller.Net
{
    public interface IAuthService
    {
        void Authenticate(IServiceRequest request,
            IAuthenticationAttributes authAttribtues);
    }
}

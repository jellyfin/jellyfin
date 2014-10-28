using ServiceStack.Web;

namespace MediaBrowser.Controller.Net
{
    public interface IAuthService
    {
        void Authenticate(IRequest request, 
            IResponse response, 
            object requestDto, 
            IAuthenticated authAttribtues);
    }
}

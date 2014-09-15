using ServiceStack.Web;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Net
{
    public interface IAuthService
    {
        void Authenticate(IRequest request, 
            IResponse response, 
            object requestDto, 
            bool allowLocal, 
            string[] roles);
    }
}

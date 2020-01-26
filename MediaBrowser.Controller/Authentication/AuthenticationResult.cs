#pragma warning disable CS1591
#pragma warning disable SA1600

using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dto;

namespace MediaBrowser.Controller.Authentication
{
    public class AuthenticationResult
    {
        public UserDto User { get; set; }

        public SessionInfo SessionInfo { get; set; }

        public string AccessToken { get; set; }

        public string ServerId { get; set; }
    }
}

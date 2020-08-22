#pragma warning disable CS1591

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

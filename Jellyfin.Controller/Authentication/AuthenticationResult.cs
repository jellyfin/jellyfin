using Jellyfin.Controller.Session;
using Jellyfin.Model.Dto;


namespace Jellyfin.Controller.Authentication
{
    public class AuthenticationResult
    {
        public UserDto User { get; set; }
        public SessionInfo SessionInfo { get; set; }
        public string AccessToken { get; set; }
        public string ServerId { get; set; }
    }
}

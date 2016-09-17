
namespace MediaBrowser.Controller.Session
{
    public class AuthenticationRequest
    {
        public string Username { get; set; }
        public string UserId { get; set; }
        public string PasswordSha1 { get; set; }
        public string PasswordMd5 { get; set; }
        public string App { get; set; }
        public string AppVersion { get; set; }
        public string DeviceId { get; set; }
        public string DeviceName { get; set; }
        public string RemoteEndPoint { get; set; }
    }
}

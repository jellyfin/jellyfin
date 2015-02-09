
namespace MediaBrowser.Model.Connect
{
    public class ConnectAuthorizationRequest
    {
        public string SendingUserId { get; set; }
        public string ConnectUserName { get; set; }
        public string[] EnabledLibraries { get; set; }
        public bool EnableLiveTv { get; set; }
        public string[] EnabledChannels { get; set; }

        public ConnectAuthorizationRequest()
        {
            EnabledLibraries = new string[] { };
            EnabledChannels = new string[] { };
        }
    }
}

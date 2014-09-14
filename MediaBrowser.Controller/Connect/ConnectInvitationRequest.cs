
namespace MediaBrowser.Controller.Connect
{
    public class ConnectInvitationRequest
    {
        public string LocalUserId { get; set; }

        public string Username { get; set; }

        public string RequesterUserId { get; set; }

        public ConnectUserType Type { get; set; }
    }

    public enum ConnectUserType
    {
        LinkedUser = 1,
        Guest = 2
    }
}

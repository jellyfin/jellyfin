
namespace MediaBrowser.Controller.Connect
{
    public class UserLinkResult
    {
        public bool IsPending { get; set; }
        public bool IsNewUserInvitation { get; set; }
        public string GuestDisplayName { get; set; }
    }
}

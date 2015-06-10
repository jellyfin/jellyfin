
namespace MediaBrowser.Model.ApiClient
{
    public class ConnectSignupResponse
    {
        public bool IsSuccessful { get; set; }
        public bool IsEmailInUse { get; set; }
        public bool IsUsernameInUse { get; set; }
    }
}

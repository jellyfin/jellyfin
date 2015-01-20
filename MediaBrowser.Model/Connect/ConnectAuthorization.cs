
namespace MediaBrowser.Model.Connect
{
    public class ConnectAuthorization
    {
        public string ConnectUserId { get; set; }
        public string UserName { get; set; }
        public string ImageUrl { get; set; }
        public string Id { get; set; }
        public string[] EnabledLibraries { get; set; }
        public bool EnableLiveTv { get; set; }
        public string[] EnabledChannels { get; set; }

        public ConnectAuthorization()
        {
            EnabledLibraries = new string[] { };
            EnabledChannels = new string[] { };
        }
    }
}

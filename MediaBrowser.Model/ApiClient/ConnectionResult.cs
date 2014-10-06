
namespace MediaBrowser.Model.ApiClient
{
    public class ConnectionResult
    {
        public ConnectionState State { get; set; }
        public ServerInfo ServerInfo { get; set; }
        public IApiClient ApiClient { get; set; }

        public ConnectionResult()
        {
            State = ConnectionState.Unavailable;
        }
    }

    public enum ConnectionState
    {
        Unavailable = 1,
        ServerSignIn = 2,
        SignedIn = 3
    }

    public enum ConnectionMode
    {
        Local = 1,
        Remote = 2
    }
}

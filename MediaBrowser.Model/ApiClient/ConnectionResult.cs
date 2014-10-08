
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
}

using MediaBrowser.Model.Connect;
using MediaBrowser.Model.Dto;
using System.Collections.Generic;

namespace MediaBrowser.Model.ApiClient
{
    public class ConnectionResult
    {
        public ConnectionState State { get; set; }
        public List<ServerInfo> Servers { get; set; }
        public IApiClient ApiClient { get; set; }
        public ConnectUser ConnectUser { get; set; }
        public UserDto OfflineUser { get; set; }

        public ConnectionResult()
        {
            State = ConnectionState.Unavailable;
            Servers = new List<ServerInfo>();
        }
    }
}

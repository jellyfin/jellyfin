using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.ApiClient
{
    public class ServerInfo
    {
        public String Name { get; set; }
        public String Id { get; set; }
        public String LocalAddress { get; set; }
        public String RemoteAddress { get; set; }
        public String UserId { get; set; }
        public String AccessToken { get; set; }
        public List<WakeOnLanInfo> WakeOnLanInfos { get; set; }

        public ServerInfo()
        {
            WakeOnLanInfos = new List<WakeOnLanInfo>();
            LocalAddress = "http://localhost:8096";
        }
    }

    public class WakeOnLanInfo
    {
        public string MacAddress { get; set; }
        public int Port { get; set; }

        public WakeOnLanInfo()
        {
            Port = 9;
        }
    }
}

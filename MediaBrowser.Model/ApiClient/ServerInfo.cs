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
        public DateTime DateLastAccessed { get; set; }
        public String ExchangeToken { get; set; }

        public ServerInfo()
        {
            WakeOnLanInfos = new List<WakeOnLanInfo>();
        }
    }
}

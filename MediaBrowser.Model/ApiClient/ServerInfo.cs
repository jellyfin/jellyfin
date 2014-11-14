using MediaBrowser.Model.Connect;
using MediaBrowser.Model.System;
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
        public UserLinkType? UserLinkType { get; set; }

        public bool IsLocalAddressFixed { get; set; }

        public ServerInfo()
        {
            WakeOnLanInfos = new List<WakeOnLanInfo>();
        }

        public void ImportInfo(PublicSystemInfo systemInfo)
        {
            Name = systemInfo.ServerName;
            Id = systemInfo.Id;

            if (!IsLocalAddressFixed && !string.IsNullOrEmpty(systemInfo.LocalAddress))
            {
                LocalAddress = systemInfo.LocalAddress;
            }

            if (!string.IsNullOrEmpty(systemInfo.WanAddress))
            {
                RemoteAddress = systemInfo.WanAddress;
            }

            var fullSystemInfo = systemInfo as SystemInfo;

            if (fullSystemInfo != null)
            {
                WakeOnLanInfos = new List<WakeOnLanInfo>();

                if (!string.IsNullOrEmpty(fullSystemInfo.MacAddress))
                {
                    WakeOnLanInfos.Add(new WakeOnLanInfo
                    {
                        MacAddress = fullSystemInfo.MacAddress
                    });
                }
            }
        }
    }
}

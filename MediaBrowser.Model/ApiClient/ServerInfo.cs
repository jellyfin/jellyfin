using MediaBrowser.Model.Connect;
using MediaBrowser.Model.Extensions;
using MediaBrowser.Model.System;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.ApiClient
{
    public class ServerInfo
    {
        public List<ServerUserInfo> Users { get; set; }
        
        public String Name { get; set; }
        public String Id { get; set; }
        public String ConnectServerId { get; set; }
        public String LocalAddress { get; set; }
        public String RemoteAddress { get; set; }
        public String ManualAddress { get; set; }
        public String UserId { get; set; }
        public String AccessToken { get; set; }
        public List<WakeOnLanInfo> WakeOnLanInfos { get; set; }
        public DateTime DateLastAccessed { get; set; }
        public String ExchangeToken { get; set; }
        public UserLinkType? UserLinkType { get; set; }
        public ConnectionMode? LastConnectionMode { get; set; }

        public ServerInfo()
        {
            WakeOnLanInfos = new List<WakeOnLanInfo>();
            Users = new List<ServerUserInfo>();
        }

        public void ImportInfo(PublicSystemInfo systemInfo)
        {
            Name = systemInfo.ServerName;
            Id = systemInfo.Id;

            if (!string.IsNullOrEmpty(systemInfo.LocalAddress))
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

        public string GetAddress(ConnectionMode mode)
        {
            switch (mode)
            {
                case ConnectionMode.Local:
                    return LocalAddress;
                case ConnectionMode.Manual:
                    return ManualAddress;
                case ConnectionMode.Remote:
                    return RemoteAddress;
                default:
                    throw new ArgumentException("Unexpected ConnectionMode");
            }
        }

        public void AddOrUpdate(ServerUserInfo user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            // Clone the existing list of users
            var list = new List<ServerUserInfo>();
            foreach (ServerUserInfo serverUserInfo in Users)
            {
                list.Add(serverUserInfo);
            }

            var index = FindIndex(list, user.Id);

            if (index != -1)
            {
                var existing = list[index];

                // Merge the data
                existing.IsSignedInOffline = user.IsSignedInOffline;
            }
            else
            {
                list.Add(user);
            }

            Users = list;
        }

        private int FindIndex(List<ServerUserInfo> users, string id)
        {
            var index = 0;

            foreach (var user in users)
            {
                if (StringHelper.EqualsIgnoreCase(id, user.Id))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }
    }
}

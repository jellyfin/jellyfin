using MediaBrowser.Model.Extensions;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.ApiClient
{
    public class ServerCredentials
    {
        public List<ServerInfo> Servers { get; set; }

        public string ConnectUserId { get; set; }
        public string ConnectAccessToken { get; set; }

        public ServerCredentials()
        {
            Servers = new List<ServerInfo>();
        }

        public void AddOrUpdateServer(ServerInfo server)
        {
            if (server == null)
            {
                throw new ArgumentNullException("server");
            }

            // Clone the existing list of servers
            var list = new List<ServerInfo>();
            foreach (ServerInfo serverInfo in Servers)
            {
                list.Add(serverInfo);
            }

            var index = FindIndex(list, server.Id);

            if (index != -1)
            {
                var existing = list[index];

                // Take the most recent DateLastAccessed
                if (server.DateLastAccessed > existing.DateLastAccessed)
                {
                    existing.DateLastAccessed = server.DateLastAccessed;
                }
                
                existing.UserLinkType = server.UserLinkType;

                if (!string.IsNullOrEmpty(server.AccessToken))
                {
                    existing.AccessToken = server.AccessToken;
                    existing.UserId = server.UserId;
                }
                if (!string.IsNullOrEmpty(server.ExchangeToken))
                {
                    existing.ExchangeToken = server.ExchangeToken;
                }
                if (!string.IsNullOrEmpty(server.RemoteAddress))
                {
                    existing.RemoteAddress = server.RemoteAddress;
                }
                if (!string.IsNullOrEmpty(server.ConnectServerId))
                {
                    existing.ConnectServerId = server.ConnectServerId;
                }
                if (!string.IsNullOrEmpty(server.LocalAddress))
                {
                    existing.LocalAddress = server.LocalAddress;
                }
                if (!string.IsNullOrEmpty(server.ManualAddress))
                {
                    existing.LocalAddress = server.ManualAddress;
                }
                if (!string.IsNullOrEmpty(server.Name))
                {
                    existing.Name = server.Name;
                }
                if (server.WakeOnLanInfos != null && server.WakeOnLanInfos.Count > 0)
                {
                    existing.WakeOnLanInfos = new List<WakeOnLanInfo>();
                    foreach (WakeOnLanInfo info in server.WakeOnLanInfos)
                    {
                        existing.WakeOnLanInfos.Add(info);
                    }
                }
                if (server.LastConnectionMode.HasValue)
                {
                    existing.LastConnectionMode = server.LastConnectionMode;
                }
                foreach (ServerUserInfo user in server.Users)
                {
                    existing.AddOrUpdate(user);
                }
            }
            else
            {
                list.Add(server);
            }

            Servers = list;
        }

        private int FindIndex(List<ServerInfo> servers, string id)
        {
            var index = 0;

            foreach (ServerInfo server in servers)
            {
                if (StringHelper.EqualsIgnoreCase(id, server.Id))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        public ServerInfo GetServer(string id)
        {
            foreach (ServerInfo server in Servers)
            {
                if (StringHelper.EqualsIgnoreCase(id, server.Id))
                {
                    return server;
                }
            }

            return null;
        }
    }
}

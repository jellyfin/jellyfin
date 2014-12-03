using MediaBrowser.Model.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

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

            var list = Servers.ToList();

            var index = FindIndex(list, server.Id);

            if (index != -1)
            {
                var existing = list[index];

                // Merge the data
                existing.DateLastAccessed = new[] { existing.DateLastAccessed, server.DateLastAccessed }.Max();

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
                    existing.WakeOnLanInfos = server.WakeOnLanInfos.ToList();
                }
                if (server.LastConnectionMode.HasValue)
                {
                    existing.LastConnectionMode = server.LastConnectionMode;
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

            foreach (var server in servers)
            {
                if (StringHelper.Equals(id, server.Id))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }
    }
}

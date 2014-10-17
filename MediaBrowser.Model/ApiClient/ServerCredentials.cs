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
                list[index] = server;
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

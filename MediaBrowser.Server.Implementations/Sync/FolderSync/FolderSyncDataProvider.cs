using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sync.FolderSync
{
    public class FolderSyncDataProvider : ISyncDataProvider
    {
        public Task<List<string>> GetServerItemIds(SyncTarget target, string serverId)
        {
            throw new NotImplementedException();
        }

        public Task AddOrUpdate(SyncTarget target, LocalItem item)
        {
            throw new NotImplementedException();
        }

        public Task Delete(SyncTarget target, string id)
        {
            throw new NotImplementedException();
        }

        public Task<LocalItem> Get(SyncTarget target, string id)
        {
            throw new NotImplementedException();
        }
    }
}

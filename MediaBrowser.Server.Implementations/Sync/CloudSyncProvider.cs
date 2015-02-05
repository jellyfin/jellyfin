using MediaBrowser.Common;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class CloudSyncProvider : IServerSyncProvider
    {
        private ICloudSyncProvider[] _providers = {};

        public CloudSyncProvider(IApplicationHost appHost)
        {
            _providers = appHost.GetExports<ICloudSyncProvider>().ToArray();
        }

        public IEnumerable<SyncTarget> GetSyncTargets()
        {
            return new List<SyncTarget>();
        }

        public IEnumerable<SyncTarget> GetSyncTargets(string userId)
        {
            return new List<SyncTarget>();
        }

        public DeviceProfile GetDeviceProfile(SyncTarget target)
        {
            return new DeviceProfile();
        }

        public string Name
        {
            get { return "Cloud Sync"; }
        }

        public Task<List<string>> GetServerItemIds(string serverId, SyncTarget target, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteItem(string serverId, string itemId, SyncTarget target, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task TransferItemFile(string serverId, string itemId, string path, SyncTarget target, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task TransferRelatedFile(string serverId, string itemId, string path, ItemFileType type, SyncTarget target, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

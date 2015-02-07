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
        private readonly ICloudSyncProvider[] _providers = {};

        public CloudSyncProvider(IApplicationHost appHost)
        {
            _providers = appHost.GetExports<ICloudSyncProvider>().ToArray();
        }

        public IEnumerable<SyncTarget> GetSyncTargets(string userId)
        {
            return _providers.SelectMany(i => i.GetSyncTargets(userId));
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

        public Task TransferItemFile(string serverId, string itemId, string[] pathParts, string name, ItemFileType fileType, SyncTarget target, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}

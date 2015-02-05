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

        public IEnumerable<SyncTarget> GetSyncTargets()
        {
            return _providers
                .SelectMany(i => i.GetSyncAccounts().Select(a => GetSyncTarget(i, a)));
        }

        public IEnumerable<SyncTarget> GetSyncTargets(string userId)
        {
            return _providers
                .SelectMany(i => i.GetSyncAccounts().Where(a => a.UserIds.Contains(userId, StringComparer.OrdinalIgnoreCase)).Select(a => GetSyncTarget(i, a)));
        }

        public DeviceProfile GetDeviceProfile(SyncTarget target)
        {
            return new DeviceProfile();
        }

        private SyncTarget GetSyncTarget(ICloudSyncProvider provider, SyncAccount account)
        {
            return new SyncTarget
            {
                Name = account.Name,
                Id = account.Name
            };
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

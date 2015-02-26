using MediaBrowser.Common;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;
using System.IO;
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

        private ICloudSyncProvider GetProvider(SyncTarget target)
        {
            return null;
        }

        public Task SendFile(string inputFile, string[] pathParts, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var provider = GetProvider(target);

            return provider.SendFile(inputFile, pathParts, target, progress, cancellationToken);
        }

        public Task<Stream> GetFile(string[] pathParts, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken)
        {
            var provider = GetProvider(target);

            return provider.GetFile(pathParts, target, progress, cancellationToken);
        }
    }
}

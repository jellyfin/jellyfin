using MediaBrowser.Common;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class CloudSyncProvider : ISyncProvider
    {
        private ICloudSyncProvider[] _providers = new ICloudSyncProvider[] {};

        public CloudSyncProvider(IApplicationHost appHost)
        {
            _providers = appHost.GetExports<ICloudSyncProvider>().ToArray();
        }

        public IEnumerable<SyncTarget> GetSyncTargets()
        {
            throw new NotImplementedException();
        }

        public DeviceProfile GetDeviceProfile(SyncTarget target)
        {
            throw new NotImplementedException();
        }

        public string Name
        {
            get { return "Cloud Sync"; }
        }
    }
}

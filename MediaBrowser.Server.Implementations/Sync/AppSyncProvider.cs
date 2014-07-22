using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class AppSyncProvider : ISyncProvider
    {
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
            get { return "App Sync"; }
        }
    }
}

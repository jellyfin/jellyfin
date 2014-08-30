using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Sync;
using System.Collections.Generic;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class MockSyncProvider : ISyncProvider
    {
        public string Name
        {
            get { return "Dummy Sync"; }
        }

        public IEnumerable<SyncTarget> GetSyncTargets()
        {
            return new List<SyncTarget>
            {
                new SyncTarget
                {
                     Id = "mock".GetMD5().ToString("N"),
                     Name = "Mock Sync"
                }
            };
        }

        public DeviceProfile GetDeviceProfile(SyncTarget target)
        {
            return new DeviceProfile();
        }
    }
}

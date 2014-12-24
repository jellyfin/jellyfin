using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Sync;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class AppSyncProvider : ISyncProvider, IHasUniqueTargetIds
    {
        private readonly IDeviceManager _deviceManager;

        public AppSyncProvider(IDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        public IEnumerable<SyncTarget> GetSyncTargets()
        {
            return _deviceManager.GetDevices(new DeviceQuery
            {
                SupportsSync = true

            }).Items.Select(i => new SyncTarget
            {
                Id = i.Id,
                Name = i.Name
            });
        }

        public DeviceProfile GetDeviceProfile(SyncTarget target)
        {
            return new DeviceProfile();
        }

        public string Name
        {
            get { return "App Sync"; }
        }
    }
}

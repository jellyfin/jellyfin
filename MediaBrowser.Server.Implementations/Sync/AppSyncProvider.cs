using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Sync;
using MediaBrowser.Model.Devices;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Sync
{
    public class AppSyncProvider : ISyncProvider, IHasUniqueTargetIds, IHasSyncQuality
    {
        private readonly IDeviceManager _deviceManager;

        public AppSyncProvider(IDeviceManager deviceManager)
        {
            _deviceManager = deviceManager;
        }

        public IEnumerable<SyncTarget> GetSyncTargets(string userId)
        {
            return _deviceManager.GetDevices(new DeviceQuery
            {
                SupportsSync = true,
                UserId = userId

            }).Items.Select(i => new SyncTarget
            {
                Id = i.Id,
                Name = i.Name
            });
        }

        public DeviceProfile GetDeviceProfile(SyncTarget target, string profile, string quality)
        {
            var caps = _deviceManager.GetCapabilities(target.Id);

            var deviceProfile = caps == null || caps.DeviceProfile == null ? new DeviceProfile() : caps.DeviceProfile;
            var maxBitrate = deviceProfile.MaxStaticBitrate;

            if (maxBitrate.HasValue)
            {
                if (string.Equals(quality, "medium", StringComparison.OrdinalIgnoreCase))
                {
                    maxBitrate = Convert.ToInt32(maxBitrate.Value * .75);
                }
                else if (string.Equals(quality, "low", StringComparison.OrdinalIgnoreCase))
                {
                    maxBitrate = Convert.ToInt32(maxBitrate.Value * .5);
                }

                deviceProfile.MaxStaticBitrate = maxBitrate;
            }

            return deviceProfile;
        }

        public string Name
        {
            get { return "App Sync"; }
        }

        public IEnumerable<SyncTarget> GetAllSyncTargets()
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

        public IEnumerable<SyncQualityOption> GetQualityOptions(SyncTarget target)
        {
            return new List<SyncQualityOption>
            {
                new SyncQualityOption
                {
                    Name = SyncQuality.Original.ToString(),
                    Id = SyncQuality.Original.ToString()
                },
                new SyncQualityOption
                {
                    Name = SyncQuality.High.ToString(),
                    Id = SyncQuality.High.ToString(),
                    IsDefault = true
                },
                new SyncQualityOption
                {
                    Name = SyncQuality.Medium.ToString(),
                    Id = SyncQuality.Medium.ToString()
                },
                new SyncQualityOption
                {
                    Name = SyncQuality.Low.ToString(),
                    Id = SyncQuality.Low.ToString()
                }
            };
        }

        public IEnumerable<SyncQualityOption> GetProfileOptions(SyncTarget target)
        {
            return new List<SyncQualityOption>();
        }
    }
}

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
    public class AppSyncProvider : ISyncProvider, IHasUniqueTargetIds, IHasSyncQuality, IHasDuplicateCheck
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
            deviceProfile.MaxStaticBitrate = SyncHelper.AdjustBitrate(deviceProfile.MaxStaticBitrate, quality);

            return deviceProfile;
        }

        public string Name
        {
            get { return "Mobile Sync"; }
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
                    Name = "Original",
                    Id = "original",
                    Description = "Syncs original files as-is, regardless of whether the device is capable of playing them or not."
                },
                new SyncQualityOption
                {
                    Name = "High",
                    Id = "high",
                    IsDefault = true
                },
                new SyncQualityOption
                {
                    Name = "Medium",
                    Id = "medium"
                },
                new SyncQualityOption
                {
                    Name = "Low",
                    Id = "low"
                },
                new SyncQualityOption
                {
                    Name = "Custom",
                    Id = "custom"
                }
            };
        }

        public IEnumerable<SyncProfileOption> GetProfileOptions(SyncTarget target)
        {
            return new List<SyncProfileOption>();
        }

        public SyncJobOptions GetSyncJobOptions(SyncTarget target, string profile, string quality)
        {
            var isConverting = !string.Equals(quality, "original", StringComparison.OrdinalIgnoreCase);

            return new SyncJobOptions
            {
                DeviceProfile = GetDeviceProfile(target, profile, quality),
                IsConverting = isConverting
            };
        }

        public bool AllowDuplicateJobItem(SyncJobItem original, SyncJobItem duplicate)
        {
            return false;
        }
    }
}

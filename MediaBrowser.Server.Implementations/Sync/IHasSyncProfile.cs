using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Sync;

namespace MediaBrowser.Server.Implementations.Sync
{
    public interface IHasSyncProfile
    {
        /// <summary>
        /// Gets the device profile.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>DeviceProfile.</returns>
        DeviceProfile GetDeviceProfile(SyncTarget target);
    }
}

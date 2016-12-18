using MediaBrowser.Model.Sync;
using System.Collections.Generic;

namespace Emby.Server.Implementations.Sync
{
    public interface IHasSyncQuality
    {
        /// <summary>
        /// Gets the device profile.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="profile">The profile.</param>
        /// <param name="quality">The quality.</param>
        /// <returns>DeviceProfile.</returns>
        SyncJobOptions GetSyncJobOptions(SyncTarget target, string profile, string quality);
        
        /// <summary>
        /// Gets the quality options.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>IEnumerable&lt;SyncQualityOption&gt;.</returns>
        IEnumerable<SyncQualityOption> GetQualityOptions(SyncTarget target);

        /// <summary>
        /// Gets the profile options.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>IEnumerable&lt;SyncQualityOption&gt;.</returns>
        IEnumerable<SyncProfileOption> GetProfileOptions(SyncTarget target);
    }
}

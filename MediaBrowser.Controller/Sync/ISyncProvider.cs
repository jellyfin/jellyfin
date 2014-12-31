using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Sync;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Sync
{
    public interface ISyncProvider
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the synchronize targets.
        /// </summary>
        /// <returns>IEnumerable&lt;SyncTarget&gt;.</returns>
        IEnumerable<SyncTarget> GetSyncTargets();

        /// <summary>
        /// Gets the synchronize targets.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>IEnumerable&lt;SyncTarget&gt;.</returns>
        IEnumerable<SyncTarget> GetSyncTargets(string userId);
        
        /// <summary>
        /// Gets the device profile.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <returns>DeviceProfile.</returns>
        DeviceProfile GetDeviceProfile(SyncTarget target);
    }

    public interface IHasUniqueTargetIds
    {
        
    }
}

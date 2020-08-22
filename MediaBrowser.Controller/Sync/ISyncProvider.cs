#pragma warning disable CS1591

using System.Collections.Generic;
using MediaBrowser.Model.Sync;

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
        /// <param name="userId">The user identifier.</param>
        /// <returns>IEnumerable&lt;SyncTarget&gt;.</returns>
        List<SyncTarget> GetSyncTargets(string userId);

        /// <summary>
        /// Gets all synchronize targets.
        /// </summary>
        /// <returns>IEnumerable&lt;SyncTarget&gt;.</returns>
        List<SyncTarget> GetAllSyncTargets();
    }
}

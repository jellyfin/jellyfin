using System.Collections.Generic;

namespace MediaBrowser.Controller.Sync
{
    public interface ICloudSyncProvider
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
        IEnumerable<SyncAccount> GetSyncAccounts();
    }
}

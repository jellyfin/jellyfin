using MediaBrowser.Model.Sync;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        /// <param name="userId">The user identifier.</param>
        /// <returns>IEnumerable&lt;SyncTarget&gt;.</returns>
        IEnumerable<SyncTarget> GetSyncTargets(string userId);

        /// <summary>
        /// Transfers the item file.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="inputFile">The input file.</param>
        /// <param name="pathParts">The path parts.</param>
        /// <param name="target">The target.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task TransferItemFile(string serverId, string itemId, string inputFile, string[] pathParts, SyncTarget target, CancellationToken cancellationToken);
    }
}

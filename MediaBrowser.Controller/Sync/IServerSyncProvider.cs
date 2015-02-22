using MediaBrowser.Model.Sync;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Sync
{
    public interface IServerSyncProvider : ISyncProvider
    {
        /// <summary>
        /// Gets the server item ids.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <param name="target">The target.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;List&lt;System.String&gt;&gt;.</returns>
        Task<List<string>> GetServerItemIds(string serverId, SyncTarget target, CancellationToken cancellationToken);

        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="target">The target.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteItem(string serverId, string itemId, SyncTarget target, CancellationToken cancellationToken);

        /// <summary>
        /// Transfers the file.
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

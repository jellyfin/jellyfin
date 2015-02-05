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
        /// <param name="path">The path.</param>
        /// <param name="target">The target.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task TransferItemFile(string serverId, string itemId, string path, SyncTarget target, CancellationToken cancellationToken);

        /// <summary>
        /// Transfers the related file.
        /// </summary>
        /// <param name="serverId">The server identifier.</param>
        /// <param name="itemId">The item identifier.</param>
        /// <param name="path">The path.</param>
        /// <param name="type">The type.</param>
        /// <param name="target">The target.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task TransferRelatedFile(string serverId, string itemId, string path, ItemFileType type, SyncTarget target, CancellationToken cancellationToken);
    }
}

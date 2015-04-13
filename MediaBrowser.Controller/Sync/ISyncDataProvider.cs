using MediaBrowser.Model.Sync;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Sync
{
    public interface ISyncDataProvider
    {
        /// <summary>
        /// Gets the local items.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="serverId">The server identifier.</param>
        /// <returns>Task&lt;List&lt;LocalItem&gt;&gt;.</returns>
        Task<List<LocalItem>> GetLocalItems(SyncTarget target, string serverId);

        /// <summary>
        /// Adds the or update.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="item">The item.</param>
        /// <returns>Task.</returns>
        Task AddOrUpdate(SyncTarget target, LocalItem item);

        /// <summary>
        /// Deletes the specified identifier.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>Task.</returns>
        Task Delete(SyncTarget target, string id);

        /// <summary>
        /// Gets the specified identifier.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="id">The identifier.</param>
        /// <returns>Task&lt;LocalItem&gt;.</returns>
        Task<LocalItem> Get(SyncTarget target, string id);

        /// <summary>
        /// Gets the cached item.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="serverId">The server identifier.</param>
        /// <param name="itemId">The item identifier.</param>
        /// <returns>Task&lt;LocalItem&gt;.</returns>
        Task<List<LocalItem>> GetItems(SyncTarget target, string serverId, string itemId);
        /// <summary>
        /// Gets the cached items by synchronize job item identifier.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="serverId">The server identifier.</param>
        /// <param name="syncJobItemId">The synchronize job item identifier.</param>
        /// <returns>Task&lt;List&lt;LocalItem&gt;&gt;.</returns>
        Task<List<LocalItem>> GetItemsBySyncJobItemId(SyncTarget target, string serverId, string syncJobItemId);
    }
}

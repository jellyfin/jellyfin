using MediaBrowser.Model.Sync;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Sync
{
    public interface ISyncDataProvider
    {
        /// <summary>
        /// Gets the server item ids.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="serverId">The server identifier.</param>
        /// <returns>Task&lt;List&lt;System.String&gt;&gt;.</returns>
        Task<List<string>> GetServerItemIds(SyncTarget target, string serverId);

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
        /// <param name="id">The identifier.</param>
        /// <returns>Task&lt;LocalItem&gt;.</returns>
        Task<LocalItem> GetCachedItem(SyncTarget target, string id);
    }
}

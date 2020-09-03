#pragma warning disable CS1591

using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Sync;

namespace MediaBrowser.Controller.Sync
{
    public interface IHasDynamicAccess
    {
        /// <summary>
        /// Gets the synced file information.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="target">The target.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;SyncedFileInfo&gt;.</returns>
        Task<SyncedFileInfo> GetSyncedFileInfo(string id, SyncTarget target, CancellationToken cancellationToken);
    }
}

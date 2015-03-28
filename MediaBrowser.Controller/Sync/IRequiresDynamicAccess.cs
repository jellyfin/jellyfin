using MediaBrowser.Model.Sync;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Sync
{
    public interface IRequiresDynamicAccess
    {
        /// <summary>
        /// Gets the file information.
        /// </summary>
        /// <param name="remotePath">The remote path.</param>
        /// <param name="target">The target.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;SendFileResult&gt;.</returns>
        Task<SendFileResult> GetFileInfo(string remotePath, SyncTarget target, CancellationToken cancellationToken);
    }
}

using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.Sync
{
    public interface IServerSyncProvider : ISyncProvider
    {
        /// <summary>
        /// Transfers the file.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="pathParts">The path parts.</param>
        /// <param name="target">The target.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task<SyncedFileInfo> SendFile(Stream stream, string[] pathParts, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the file.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="target">The target.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteFile(string id, SyncTarget target, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the file.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="target">The target.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;Stream&gt;.</returns>
        Task<Stream> GetFile(string id, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken);

        Task<QueryResult<FileSystemMetadata>> GetFiles(string[] pathParts, SyncTarget target, CancellationToken cancellationToken);
        Task<QueryResult<FileSystemMetadata>> GetFiles(SyncTarget target, CancellationToken cancellationToken);
    }

    public interface ISupportsDirectCopy
    {
        /// <summary>
        /// Sends the file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="pathParts">The path parts.</param>
        /// <param name="target">The target.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;SyncedFileInfo&gt;.</returns>
        Task<SyncedFileInfo> SendFile(string path, string[] pathParts, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken);
    }

    public interface IHasDuplicateCheck
    {
        /// <summary>
        /// Allows the duplicate job item.
        /// </summary>
        /// <param name="original">The original.</param>
        /// <param name="duplicate">The duplicate.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool AllowDuplicateJobItem(SyncJobItem original, SyncJobItem duplicate);
    }
}

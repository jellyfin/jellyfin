using MediaBrowser.Model.Querying;
using MediaBrowser.Model.Sync;
using Interfaces.IO;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

        /// <summary>
        /// Gets the files.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="target">The target.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;QueryResult&lt;FileMetadata&gt;&gt;.</returns>
        Task<QueryResult<FileMetadata>> GetFiles(FileQuery query, SyncTarget target, CancellationToken cancellationToken);
    }
}

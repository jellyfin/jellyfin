using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Model.IO;
using Jellyfin.Model.Querying;
using Jellyfin.Model.Sync;

namespace Jellyfin.Controller.Sync
{
    public interface IServerSyncProvider : ISyncProvider
    {
        /// <summary>
        /// Transfers the file.
        /// </summary>
        Task<SyncedFileInfo> SendFile(SyncJob syncJob, string originalMediaPath, Stream inputStream, bool isMedia, string[] outputPathParts, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken);

        Task<QueryResult<FileSystemMetadata>> GetFiles(string[] directoryPathParts, SyncTarget target, CancellationToken cancellationToken);
    }

    public interface ISupportsDirectCopy
    {
        /// <summary>
        /// Sends the file.
        /// </summary>
        Task<SyncedFileInfo> SendFile(SyncJob syncJob, string originalMediaPath, string inputPath, bool isMedia, string[] outputPathParts, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken);
    }
}

using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;
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
        /// <param name="inputFile">The input file.</param>
        /// <param name="path">The path.</param>
        /// <param name="target">The target.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendFile(string inputFile, string path, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Deletes the file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="target">The target.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteFile(string path, SyncTarget target, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="target">The target.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;Stream&gt;.</returns>
        Task<Stream> GetFile(string path, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the full path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="target">The target.</param>
        /// <returns>System.String.</returns>
        string GetFullPath(IEnumerable<string> path, SyncTarget target);

        /// <summary>
        /// Gets the parent directory path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="target">The target.</param>
        /// <returns>System.String.</returns>
        string GetParentDirectoryPath(string path, SyncTarget target);

        /// <summary>
        /// Gets the file system entries.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="target">The target.</param>
        /// <returns>Task&lt;List&lt;DeviceFileInfo&gt;&gt;.</returns>
        Task<List<DeviceFileInfo>> GetFileSystemEntries(string path, SyncTarget target);

        /// <summary>
        /// Gets the data provider.
        /// </summary>
        /// <returns>ISyncDataProvider.</returns>
        ISyncDataProvider GetDataProvider();
    }
}

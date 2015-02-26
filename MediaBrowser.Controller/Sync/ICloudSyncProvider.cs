using MediaBrowser.Model.Sync;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Sync
{
    public interface ICloudSyncProvider
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }

        /// <summary>
        /// Gets the synchronize targets.
        /// </summary>
        /// <param name="userId">The user identifier.</param>
        /// <returns>IEnumerable&lt;SyncTarget&gt;.</returns>
        IEnumerable<SyncTarget> GetSyncTargets(string userId);

        /// <summary>
        /// Transfers the item file.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <param name="pathParts">The path parts.</param>
        /// <param name="target">The target.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task SendFile(string inputFile, string[] pathParts, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the file.
        /// </summary>
        /// <param name="pathParts">The path parts.</param>
        /// <param name="target">The target.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;Stream&gt;.</returns>
        Task<Stream> GetFile(string[] pathParts, SyncTarget target, IProgress<double> progress, CancellationToken cancellationToken);
    }
}

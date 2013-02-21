using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.IO
{
    public interface IIsoManager : IDisposable
    {
        /// <summary>
        /// Mounts the specified iso path.
        /// </summary>
        /// <param name="isoPath">The iso path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="visibleToAllProcesses">if set to <c>true</c> [visible to all processes].</param>
        /// <returns>IsoMount.</returns>
        /// <exception cref="System.ArgumentNullException">isoPath</exception>
        /// <exception cref="System.IO.IOException">Unable to create mount.</exception>
        Task<IIsoMount> Mount(string isoPath, CancellationToken cancellationToken, bool visibleToAllProcesses = true);

        /// <summary>
        /// Determines whether this instance can mount the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if this instance can mount the specified path; otherwise, <c>false</c>.</returns>
        bool CanMount(string path);
    }
}
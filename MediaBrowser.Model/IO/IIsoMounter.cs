using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Model.IO
{
    public interface IIsoMounter : IDisposable
    {
        /// <summary>
        /// Mounts the specified iso path.
        /// </summary>
        /// <param name="isoPath">The iso path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>IsoMount.</returns>
        /// <exception cref="ArgumentNullException">isoPath</exception>
        /// <exception cref="IOException">Unable to create mount.</exception>
        Task<IIsoMount> Mount(string isoPath, CancellationToken cancellationToken);

        /// <summary>
        /// Determines whether this instance can mount the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if this instance can mount the specified path; otherwise, <c>false</c>.</returns>
        bool CanMount(string path);

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }
    }
}

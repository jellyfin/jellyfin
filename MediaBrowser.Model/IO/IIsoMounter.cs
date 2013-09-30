using System;
using System.IO;
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
        /// Gets a value indicating whether [requires installation].
        /// </summary>
        /// <value><c>true</c> if [requires installation]; otherwise, <c>false</c>.</value>
        bool RequiresInstallation { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is installed.
        /// </summary>
        /// <value><c>true</c> if this instance is installed; otherwise, <c>false</c>.</value>
        bool IsInstalled { get; }

        /// <summary>
        /// Installs this instance.
        /// </summary>
        /// <returns>Task.</returns>
        Task Install(CancellationToken cancellationToken);

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        string Name { get; }
    }
}

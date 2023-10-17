using System;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Metadata;

/// <summary>
/// A service for managing library refreshes.
/// </summary>
public interface ILibraryRefreshManager
{
    /// <summary>
    /// Starts a library scan.
    /// </summary>
    void StartScan();

    /// <summary>
    /// Reloads the root media folder.
    /// </summary>
    /// <param name="progress">The <see cref="IProgress{T}"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the library validation.</returns>
    Task ValidateMediaLibrary(IProgress<double> progress, CancellationToken cancellationToken);

    /// <summary>
    /// Refreshes the top-level folders.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the refresh.</returns>
    Task ValidateTopLibraryFolders(CancellationToken cancellationToken);
}

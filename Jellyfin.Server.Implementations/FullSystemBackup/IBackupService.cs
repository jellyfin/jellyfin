using System;
using System.IO;
using System.Threading.Tasks;

namespace Jellyfin.Server.Implementations.Backup;

/// <summary>
/// Defines an interface to restore and backup the jellyfin system.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates a new Backup zip file containing the current state of the application.
    /// </summary>
    /// <returns>A task.</returns>
    Task CreateBackupAsync();

    /// <summary>
    /// Restores an backup zip file created by jellyfin.
    /// </summary>
    /// <param name="archivePath">Path to the archive.</param>
    /// <returns>A Task.</returns>
    /// <exception cref="FileNotFoundException">Thrown when an invalid or missing file is specified.</exception>
    /// <exception cref="NotSupportedException">Thrown when attempt to load an unsupported backup is made.</exception>
    /// <exception cref="InvalidOperationException">Thrown for errors during the restore.</exception>
    Task RestoreBackupAsync(string archivePath);

    /// <summary>
    /// Schedules a Restore and restarts the server.
    /// </summary>
    /// <param name="archivePath">The path to the archive to restore from.</param>
    void ScheduleRestoreAndRestartServer(string archivePath);
}

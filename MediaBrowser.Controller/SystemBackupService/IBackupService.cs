using System;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Controller.SystemBackupService;

namespace Jellyfin.Server.Implementations.SystemBackupService;

/// <summary>
/// Defines an interface to restore and backup the jellyfin system.
/// </summary>
public interface IBackupService
{
    /// <summary>
    /// Creates a new Backup zip file containing the current state of the application.
    /// </summary>
    /// <param name="backupOptions">The backup options.</param>
    /// <returns>A task.</returns>
    Task<BackupManifestDto> CreateBackupAsync(BackupOptionsDto backupOptions);

    /// <summary>
    /// Gets a list of backups that are available to be restored from.
    /// </summary>
    /// <returns>A list of backup paths.</returns>
    Task<BackupManifestDto[]> EnumerateBackups();

    /// <summary>
    /// Gets a single backup manifest if the path defines a valid Jellyfin backup archive.
    /// </summary>
    /// <param name="archivePath">The path to be loaded.</param>
    /// <returns>The containing backup manifest or null if not existing or compatiable.</returns>
    Task<BackupManifestDto?> GetBackupManifest(string archivePath);

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

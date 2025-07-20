using System;
using MediaBrowser.Common.Configuration;

namespace MediaBrowser.Controller.SystemBackupService;

/// <summary>
/// Defines properties used to start a restore process.
/// </summary>
public class BackupRestoreRequestDto
{
    /// <summary>
    /// Gets or Sets the name of the backup archive to restore from. Must be present in <see cref="IApplicationPaths.BackupPath"/>.
    /// </summary>
    public required string ArchiveFileName { get; set; }
}

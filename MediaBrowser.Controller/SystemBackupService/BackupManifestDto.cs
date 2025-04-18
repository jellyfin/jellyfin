using System;

namespace MediaBrowser.Controller.SystemBackupService;

/// <summary>
/// Manifest type for backups internal structure.
/// </summary>
public class BackupManifestDto
{
    /// <summary>
    /// Gets or Sets the jellyfin version this backup was created with.
    /// </summary>
    public required Version JellyfinVersion { get; set; }

    /// <summary>
    /// Gets or Sets the backup engine version this backup was created with.
    /// </summary>
    public required Version BackupEngineVersion { get; set; }

    /// <summary>
    /// Gets or Sets the date this backup was created with.
    /// </summary>
    public required DateTimeOffset DateOfCreation { get; set; }
}

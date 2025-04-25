using System;

namespace MediaBrowser.Controller.SystemBackupService;

/// <summary>
/// Manifest type for backups internal structure.
/// </summary>
public class BackupManifestDto
{
    /// <summary>
    /// Gets or sets the jellyfin version this backup was created with.
    /// </summary>
    public required Version ServerVersion { get; set; }

    /// <summary>
    /// Gets or sets the backup engine version this backup was created with.
    /// </summary>
    public required Version BackupEngineVersion { get; set; }

    /// <summary>
    /// Gets or sets the date this backup was created with.
    /// </summary>
    public required DateTimeOffset DateCreated { get; set; }

    /// <summary>
    /// Gets or sets the path to the backup on the system.
    /// </summary>
    public required string Path { get; set; }

    /// <summary>
    /// Gets or sets the contents of the backup archive.
    /// </summary>
    public required BackupOptionsDto Options { get; set; }
}

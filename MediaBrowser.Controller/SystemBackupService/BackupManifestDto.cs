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
    public required Version JellyfinVersion { get; set; }

    /// <summary>
    /// Gets or sets the backup engine version this backup was created with.
    /// </summary>
    public required Version BackupEngineVersion { get; set; }

    /// <summary>
    /// Gets or sets the date this backup was created with.
    /// </summary>
    public required DateTimeOffset DateOfCreation { get; set; }

    /// <summary>
    /// Gets or sets the path to the backup on the system.
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Gets or sets the contents of the backup archive.
    /// </summary>
    public required BackupOptionsDto ContentOptions { get; set; }
}

using System;

namespace Jellyfin.Server.Implementations.Backup;

/// <summary>
/// Manifest type for backups internal structure.
/// </summary>
internal class BackupManifest
{
    public required Version Version { get; set; }

    public required DateTimeOffset DateOfCreation { get; set; }

    public required string[] DatabaseTables { get; set; }
}

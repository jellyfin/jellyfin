using System;

namespace Jellyfin.Server.Implementations.Backup;

/// <summary>
/// Manifest type for backups internal structure.
/// </summary>
internal class BackupManifest
{
    public required Version JellyfinVersion { get; set; }

    public required Version BackupEngineVersion { get; set; }

    public required DateTimeOffset DateOfCreation { get; set; }

    public required string[] DatabaseTables { get; set; }

    public required BackupOptions ContentOptions { get; set; }
}

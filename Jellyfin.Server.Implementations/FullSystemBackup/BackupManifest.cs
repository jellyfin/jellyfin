using System;

namespace Jellyfin.Server.Implementations.FullSystemBackup;

/// <summary>
/// Manifest type for backups internal structure.
/// </summary>
internal class BackupManifest
{
    public required Version ServerVersion { get; set; }

    public required Version BackupEngineVersion { get; set; }

    public required DateTimeOffset DateCreated { get; set; }

    public required string[] DatabaseTables { get; set; }

    public required BackupOptions Options { get; set; }
}

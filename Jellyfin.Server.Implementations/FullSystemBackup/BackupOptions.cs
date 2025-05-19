namespace Jellyfin.Server.Implementations.FullSystemBackup;

/// <summary>
/// Defines the optional contents of the backup archive.
/// </summary>
internal class BackupOptions
{
    public bool Metadata { get; set; }

    public bool Trickplay { get; set; }

    public bool Subtitles { get; set; }

    public bool Database { get; set; }
}

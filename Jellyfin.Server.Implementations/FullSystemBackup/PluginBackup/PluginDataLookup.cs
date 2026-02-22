namespace Jellyfin.Server.Implementations.FullSystemBackup;

internal class PluginDataLookup
{
    public required string Key { get; set; }

    public required string BackupDataFqtn { get; set; }

    public required string Metadata { get; set; }
}

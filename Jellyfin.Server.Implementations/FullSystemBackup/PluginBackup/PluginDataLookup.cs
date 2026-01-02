using System;
using System.Collections.Generic;
using MediaBrowser.Controller.SystemBackupService;

namespace Jellyfin.Server.Implementations.FullSystemBackup;

internal class PluginDataLookup
{
    public required string Key { get; set; }

    public required string BackupDataFqtn { get; set; }

    public required string Metadata { get; set; }
}

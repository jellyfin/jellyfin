using System;
using System.Collections.Generic;

namespace Jellyfin.Server.Implementations.FullSystemBackup;

/// <summary>
/// Defines a set of data stored in a backup for a plugin.
/// </summary>
internal class PluginBackupManifest
{
    public Guid PluginId { get; set; }

    public IList<PluginDataLookup> PluginDataLookup { get; set; } = [];
}

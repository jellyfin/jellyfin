using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Jellyfin.Server.Implementations.FullSystemBackup;

/// <summary>
/// Defines a plugin that can provide and read data from the plugin system.
/// </summary>
public interface IPluginBackupService
{
    /// <summary>
    /// Produces a set of data from the Plugin that will be included in the full system backup.
    /// </summary>
    /// <returns>A dictionary of keyed plugin data entries.</returns>
    ValueTask<IDictionary<string, IPluginDataEntry>> BackupData();

    /// <summary>
    /// Restores the plugin data from a backup.
    /// </summary>
    /// <param name="pluginData">The Plugin data.</param>
    /// <returns>A task that completes when the restore operation has finished.</returns>
    ValueTask RestoreData(IDictionary<string, IPluginDataEntry> pluginData);
}

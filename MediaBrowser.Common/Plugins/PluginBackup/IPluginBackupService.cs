using System.Threading.Tasks;

namespace MediaBrowser.Common.Plugins.Backup;

/// <summary>
/// Defines a plugin that can provide and read data from the plugin system.
/// </summary>
public interface IPluginBackupService
{
    /// <summary>
    /// Produces a set of data from the Plugin that will be included in the full system backup.
    /// </summary>
    /// <param name="pluginBackupDataset">The Plugin data.</param>
    /// <returns>A dictionary of keyed plugin data entries.</returns>
    ValueTask BackupData(IPluginBackupDatasetWriter pluginBackupDataset);

    /// <summary>
    /// Restores the plugin data from a backup.
    /// </summary>
    /// <param name="pluginBackupDataset">The Plugin data.</param>
    /// <returns>A task that completes when the restore operation has finished.</returns>
    ValueTask RestoreData(IPluginBackupDatasetReader pluginBackupDataset);
}

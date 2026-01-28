using System.Threading.Tasks;

namespace Jellyfin.Server.Implementations.FullSystemBackup;

/// <summary>
/// Defines methods for plugins to be aware of a pending backup operation.
/// </summary>
public interface IBackupAwarePlugin
{
    /// <summary>
    /// Gets invoked when a backup of this plugin is pending.
    /// </summary>
    /// <returns>A task that should complete when the plugin can be backuped.</returns>
    ValueTask SignalBackupPending();

    /// <summary>
    /// Get invoked when a backup of the plugin has been done.
    /// </summary>
    /// <returns>A task that completes once the plugin has resumed.</returns>
    ValueTask SignalBackupDone();
}

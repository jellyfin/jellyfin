using System;

namespace MediaBrowser.Controller.SystemBackupService;

/// <summary>
/// Contains informations about plugin data stored in a backup.
/// </summary>
public class PluginBackupManifestDto
{
    /// <summary>
    /// Gets or sets the id of the plugin.
    /// </summary>
    public Guid PluginId { get; set; }
}

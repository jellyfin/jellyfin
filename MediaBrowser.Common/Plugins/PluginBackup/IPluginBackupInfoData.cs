using System;

namespace Jellyfin.Server.Implementations.FullSystemBackup;

/// <summary>
/// Defines the attribute used to mark a plugin with backup support.
/// [Internal use only].
/// </summary>
public interface IPluginBackupInfoData
{
    /// <summary>
    /// Gets the Loader type.
    /// </summary>
    Type LoaderType { get; }

    /// <summary>
    /// Gets the plugin Id.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the common plugin name.
    /// </summary>
    string Name { get; }
}

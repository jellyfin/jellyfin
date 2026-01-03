using System;
using System.IO;

namespace MediaBrowser.Common.Plugins.Backup;

/// <summary>
/// Contains information about the plugins backup dataset.
/// </summary>
public interface IPluginBackupDatasetWriter
{
    /// <summary>
    /// Writes a Blob to the backup system.
    /// </summary>
    /// <param name="key">The backup key.</param>
    /// <param name="value">The stream binary data that should be written to the backup system.</param>
    void WriteAsBlob(string key, Stream value);

    /// <summary>
    /// Writes an object in a serialised form to the backup system. This object should be fully serialiable with common serializers like json or xml.
    /// </summary>
    /// <param name="key">The backup key.</param>
    /// <param name="value">The value to serialise and write to the backup.</param>
    void WriteAsObject(string key, object value);

    /// <summary>
    /// Stores all files within a directory and subdirectories to the backup.
    /// </summary>
    /// <param name="key">The backup key.</param>
    /// <param name="sourceDirectory">The Directory Info to backup.</param>
    /// <param name="filter">A filter callback to exclude certain files from the backup.</param>
    void WriteDirectory(string key, DirectoryInfo sourceDirectory, Func<string, bool>? filter = null);

    /// <summary>
    /// Writes a short string to the backup for a plugin. The string should not exceed an unreasonable length.
    /// </summary>
    /// <param name="key">The backup key.</param>
    /// <param name="value">The value to write to the backup.</param>
    void WriteString(string key, string value);
}

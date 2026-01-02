using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup.DataEntries;

/// <summary>
/// Provides methods for reading and writing data to the backup system.
/// </summary>
public static class BackupExtensions
{
    /// <summary>
    /// Reads a string value from the backup dataset.
    /// </summary>
    /// <param name="set">The source dataset.</param>
    /// <param name="key">The backup key.</param>
    /// <returns>The value from the backup set or null if not found.</returns>
    public static string? ReadAsString(this IReadOnlyDictionary<string, IPluginDataEntry> set, string key)
    {
        return set.TryGetValue(key, out var reader) ? (reader as StringDataEntry)?.Value : null;
    }

    /// <summary>
    /// Writes a short string to the backup for a plugin. The string should not exceed an unreasonable length.
    /// </summary>
    /// <param name="set">The source dataset.</param>
    /// <param name="key">The backup key.</param>
    /// <param name="value">The value to write to the backup.</param>
    public static void WriteString(this IDictionary<string, IPluginDataEntry> set, string key, string value)
    {
        set[key] = new StringDataEntry(value);
    }

    /// <summary>
    /// Reads an object serialized from the backup set.
    /// </summary>
    /// <typeparam name="T">The type the object should be deserialised as.</typeparam>
    /// <param name="set">The source dataset.</param>
    /// <param name="key">The backup key.</param>
    /// <returns>The value from the backup set or null if not found.</returns>
    public static T? ReadAsObject<T>(this IReadOnlyDictionary<string, IPluginDataEntry> set, string key)
        where T : class
    {
        return set.TryGetValue(key, out var reader) ? (reader as ObjectDataReader)?.ReadAs<T>() : default;
    }

    /// <summary>
    /// Writes an object in a serialised form to the backup system. This object should be fully serialiable with common serializers like json or xml.
    /// </summary>
    /// <param name="set">The source dataset.</param>
    /// <param name="key">The backup key.</param>
    /// <param name="value">The value to serialise and write to the backup.</param>
    public static void WriteAsObject(this IDictionary<string, IPluginDataEntry> set, string key, object value)
    {
        set[key] = new ObjectDataWriter(value);
    }

    /// <summary>
    /// Reads an Blob from the backup.
    /// </summary>
    /// <param name="set">The source dataset.</param>
    /// <param name="key">The backup key.</param>
    /// <returns>A stream containing the binary data from the backup.</returns>
    public static Stream? ReadAsBlob(this IReadOnlyDictionary<string, IPluginDataEntry> set, string key)
    {
        return set.TryGetValue(key, out var reader) ? (reader as FileDataReader)?.GetStream() : default;
    }

    /// <summary>
    /// Writes a Blob to the backup system.
    /// </summary>
    /// <param name="set">The source dataset.</param>
    /// <param name="key">The backup key.</param>
    /// <param name="value">The stream binary data that should be written to the backup system.</param>
    public static void WriteAsBlob(this IDictionary<string, IPluginDataEntry> set, string key, Stream value)
    {
        set[key] = new FileDataWriter(value);
    }

    /// <summary>
    /// Restores a directory structure from the backup system to an exisiting folder on the filesystem. Existing files will be overwritten.
    /// </summary>
    /// <param name="set">The source dataset.</param>
    /// <param name="key">The backup key.</param>
    /// <param name="targetDirectory">The directory info where the backup should be written to.</param>
    /// <returns>A task that completes once the restore has been completed.</returns>
    public static async Task ReadDirectory(this IReadOnlyDictionary<string, IPluginDataEntry> set, string key, DirectoryInfo targetDirectory)
    {
        if (set.TryGetValue(key, out var reader) && reader is DirectoryDataReader)
        {
            await ((DirectoryDataReader)reader).RestoreDirectory(targetDirectory.FullName).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Stores all files within a directory and subdirectories to the backup.
    /// </summary>
    /// <param name="set">The source dataset.</param>
    /// <param name="key">The backup key.</param>
    /// <param name="sourceDirectory">The Directory Info to backup.</param>
    /// <param name="filter">A filter callback to exclude certain files from the backup.</param>
    public static void WriteDirectory(this IDictionary<string, IPluginDataEntry> set, string key, DirectoryInfo sourceDirectory, Func<string, bool>? filter = null)
    {
        set[key] = new DirectoryDataWriter(sourceDirectory.FullName)
        {
            Filter = filter
        };
    }
}

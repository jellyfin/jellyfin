using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Plugins.Backup;

/// <summary>
/// Contains information about the plugins backup dataset.
/// </summary>
public interface IPluginBackupDatasetReader
{
    /// <summary>
    /// Reads an Blob from the backup.
    /// </summary>
    /// <param name="key">The backup key.</param>
    /// <returns>A stream containing the binary data from the backup.</returns>
    Stream? ReadAsBlob(string key);

    /// <summary>
    /// Reads an object serialized from the backup set.
    /// </summary>
    /// <typeparam name="T">The type the object should be deserialised as.</typeparam>
    /// <param name="key">The backup key.</param>
    /// <returns>The value from the backup set or null if not found.</returns>
    T? ReadAsObject<T>(string key)
        where T : class;

    /// <summary>
    /// Reads a string value from the backup dataset.
    /// </summary>
    /// <param name="key">The backup key.</param>
    /// <returns>The value from the backup set or null if not found.</returns>
    string? ReadAsString(string key);

    /// <summary>
    /// Restores a directory structure from the backup system to an exisiting folder on the filesystem. Existing files will be overwritten.
    /// </summary>
    /// <param name="key">The backup key.</param>
    /// <param name="targetDirectory">The directory info where the backup should be written to.</param>
    /// <returns>A task that completes once the restore has been completed.</returns>
    Task ReadDirectory(string key, DirectoryInfo targetDirectory);
}

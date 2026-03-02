using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup.DataEntries;
using MediaBrowser.Common.Plugins.Backup;

namespace Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup;

internal class PluginBackupDataset : IPluginBackupDatasetReader, IPluginBackupDatasetWriter
{
    private IDictionary<string, IPluginDataEntry> _dataset;

    public PluginBackupDataset(IDictionary<string, IPluginDataEntry> backupSet)
    {
        _dataset = backupSet;
    }

    public string? ReadAsString(string key)
    {
        return _dataset.TryGetValue(key, out var reader) ? (reader as StringDataEntry)?.Value : null;
    }

    public void WriteString(string key, string value)
    {
        _dataset[key] = new StringDataEntry(value);
    }

    public T? ReadAsObject<T>(string key)
        where T : class
    {
        return _dataset.TryGetValue(key, out var reader) ? (reader as ObjectDataReader)?.ReadAs<T>() : default;
    }

    public void WriteAsObject(string key, object value)
    {
        _dataset[key] = new ObjectDataWriter(value);
    }

    public Stream? ReadAsBlob(string key)
    {
        return _dataset.TryGetValue(key, out var reader) ? (reader as FileDataReader)?.GetStream() : default;
    }

    public void WriteAsBlob(string key, Stream value)
    {
        _dataset[key] = new FileDataWriter(value);
    }

    public async Task ReadDirectory(string key, DirectoryInfo targetDirectory)
    {
        if (_dataset.TryGetValue(key, out var reader) && reader is DirectoryDataReader reader1)
        {
            await reader1.RestoreDirectory(targetDirectory.FullName).ConfigureAwait(false);
        }
    }

    public void WriteDirectory(string key, DirectoryInfo sourceDirectory, Func<string, bool>? filter = null)
    {
        _dataset[key] = new DirectoryDataWriter(sourceDirectory.FullName)
        {
            Filter = filter
        };
    }
}

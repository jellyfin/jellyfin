using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;

namespace Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup.DataEntries;

/// <summary>
/// Defines methods to store a dataset via a <see cref="Stream"/>.
/// </summary>
public abstract class LazySingleFileBackedDataEntryBase : IPluginDataHandling
{
    private ZipArchive? _zipArchive;
    private string? _metadata;

    /// <summary>
    /// Should provide a stream of the data to backup.
    /// </summary>
    /// <returns>A stream containing the backup data.</returns>
    /// <returns>A task that completes ones the deserialisation is complete.</returns>
    protected abstract ValueTask<Stream> GetDataStream();

    /// <summary>
    /// Provides a stream containing the backup data directly from the archive.
    /// </summary>
    /// <returns>A task that resolves with a Stream once the stream from the archive has been opened.</returns>
    protected virtual ValueTask<Stream> GetStoredDataStream()
    {
        if (_zipArchive is null || string.IsNullOrWhiteSpace(_metadata))
        {
            throw new InvalidOperationException("Tried to load data without providing a valid archive");
        }

        return ValueTask.FromResult(_zipArchive.GetEntry(_metadata)!.Open());
    }

    async ValueTask<string> IPluginDataHandling.BackupData(ZipArchive zipArchive, IPlugin plugin)
    {
        var fileGuid = Guid.NewGuid().ToString("g");
        var metaReference = $"plugin/{plugin.Id}/{fileGuid}";

        using (var archiveStream = zipArchive.CreateEntry(metaReference).Open())
        {
            var dataStream = await GetDataStream().ConfigureAwait(false);
            await using (dataStream)
            {
                await dataStream.CopyToAsync(archiveStream).ConfigureAwait(false);
                dataStream.Close();
            }

            archiveStream.Close();
        }

        return fileGuid;
    }

    ValueTask IPluginDataHandling.RestoreData(ZipArchive zipArchive, string metadata)
    {
        _zipArchive = zipArchive;
        _metadata = metadata;
        return ValueTask.CompletedTask;
    }
}

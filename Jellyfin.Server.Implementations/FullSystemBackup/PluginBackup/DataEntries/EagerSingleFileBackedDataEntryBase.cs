using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using MediaBrowser.Common.Plugins;

namespace Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup.DataEntries;

/// <summary>
/// Defines methods to store a dataset via a <see cref="Stream"/>.
/// </summary>
public abstract class EagerSingleFileBackedDataEntryBase : IPluginDataHandling
{
    /// <summary>
    /// Should provide a stream of the data to backup.
    /// </summary>
    /// <returns>A stream containing the backup data.</returns>
    /// <returns>A task that completes ones the deserialisation is complete.</returns>
    protected abstract ValueTask<Stream> GetDataStream();

    /// <summary>
    /// Should deserialize the data from the backup stream back into the original format.
    /// </summary>
    /// <param name="dataStream">The data stream from the backup.</param>
    /// <returns>A task that completes ones the deserialisation is complete.</returns>
    protected abstract ValueTask LoadFromDataStream(Stream dataStream);

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

    async ValueTask IPluginDataHandling.RestoreData(ZipArchive zipArchive, string metadata)
    {
        using (var archiveStream = zipArchive.GetEntry(metadata)!.Open())
        {
            await LoadFromDataStream(archiveStream).ConfigureAwait(false);
        }
    }
}

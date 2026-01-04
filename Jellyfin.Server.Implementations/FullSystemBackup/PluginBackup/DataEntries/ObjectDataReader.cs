using System;
using System.IO.Compression;
using System.Text.Json;

namespace Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup.DataEntries;

internal class ObjectDataReader : IPluginDataReader
{
    private readonly ZipArchive _zipArchive;
    private readonly string _metadata;
    private readonly Guid _pluginId;

    public ObjectDataReader(ZipArchive zipArchive, string metadata, Guid pluginId)
    {
        _zipArchive = zipArchive;
        _metadata = metadata;
        _pluginId = pluginId;
    }

    public T? ReadAs<T>()
    {
        using (var archiveStream = _zipArchive.GetEntry($"plugin/{_pluginId:N}/{_metadata}")!.Open())
        {
            return JsonSerializer.Deserialize<T>(archiveStream);
        }
    }
}

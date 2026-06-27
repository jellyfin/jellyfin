using System;
using System.IO;
using System.IO.Compression;

namespace Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup.DataEntries;

internal class FileDataReader : IPluginDataReader
{
    private readonly ZipArchive _zipArchive;
    private readonly string _metadata;
    private readonly Guid _pluginId;

    public FileDataReader(ZipArchive zipArchive, string metadata, Guid pluginId)
    {
        _zipArchive = zipArchive;
        _metadata = metadata;
        _pluginId = pluginId;
    }

    internal Stream? GetStream()
    {
        return _zipArchive.GetEntry($"plugin/{_pluginId:N}/{_metadata}")?.Open();
    }
}

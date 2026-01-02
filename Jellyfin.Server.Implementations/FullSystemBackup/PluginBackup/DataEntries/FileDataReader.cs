using System;
using System.IO;
using System.IO.Compression;

namespace Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup.DataEntries;

internal class FileDataReader : IPluginDataReader
{
    private readonly ZipArchive _zipArchive;
    private readonly string _metadata;

    public FileDataReader(ZipArchive zipArchive, string metadata)
    {
        _zipArchive = zipArchive;
        _metadata = metadata;
    }

    internal Stream? GetStream()
    {
        return _zipArchive.GetEntry(_metadata)?.Open();
    }
}

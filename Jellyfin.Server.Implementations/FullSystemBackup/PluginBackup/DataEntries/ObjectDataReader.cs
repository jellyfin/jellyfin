using System.IO.Compression;
using System.Text.Json;

namespace Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup.DataEntries;

internal class ObjectDataReader : IPluginDataReader
{
    private readonly ZipArchive _zipArchive;
    private readonly string _metadata;

    public ObjectDataReader(ZipArchive zipArchive, string metadata)
    {
        _zipArchive = zipArchive;
        _metadata = metadata;
    }

    public T? ReadAs<T>()
    {
        using (var archiveStream = _zipArchive.GetEntry(_metadata)!.Open())
        {
            return JsonSerializer.Deserialize<T>(archiveStream);
        }
    }
}

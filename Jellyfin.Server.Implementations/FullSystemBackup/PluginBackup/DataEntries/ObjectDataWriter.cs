using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup.DataEntries;

internal class ObjectDataWriter : LazySingleFileBackedDataWriterBase
{
    private readonly object _objectData;

    public ObjectDataWriter(object objectData)
    {
        _objectData = objectData;
    }

    public override Type ReaderType => typeof(ObjectDataReader);

    protected override async ValueTask<Stream> GetDataStream()
    {
        var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, _objectData).ConfigureAwait(false);
        return stream;
    }
}

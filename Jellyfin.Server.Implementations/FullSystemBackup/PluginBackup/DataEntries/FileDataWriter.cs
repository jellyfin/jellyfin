using System;
using System.IO;
using System.Threading.Tasks;

namespace Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup.DataEntries;

/// <summary>
/// Defines methods to store a raw stream that is serializable in a backup.
/// </summary>
internal class FileDataWriter : LazySingleFileBackedDataWriterBase
{
    private readonly Stream? _stream;

    public FileDataWriter(Stream stream)
    {
        _stream = stream;
    }

    public override Type ReaderType => typeof(FileDataReader);

    /// <inheritdoc/>
    protected override ValueTask<Stream> GetDataStream()
    {
        return ValueTask.FromResult(_stream ?? throw new InvalidDataException("you need to provide a valid data stream to backup."));
    }
}

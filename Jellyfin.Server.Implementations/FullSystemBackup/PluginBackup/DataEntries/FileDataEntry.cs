using System.IO;
using System.Threading.Tasks;

namespace Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup.DataEntries;

/// <summary>
/// Defines methods to store a raw stream that is serializable in a backup.
/// </summary>
public class FileDataEntry : LazySingleFileBackedDataEntryBase
{
    private readonly Stream? _stream;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDataEntry"/> class.
    /// </summary>
    /// <param name="stream">The stream to serialise.</param>
    public FileDataEntry(Stream stream)
    {
        _stream = stream;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDataEntry"/> class.
    /// </summary>
    public FileDataEntry()
    {
    }

    /// <summary>
    /// Provides a stream from the backup archive on restore.
    /// </summary>
    /// <returns>A stream.</returns>
    public async ValueTask<Stream> ReadData()
    {
        return await GetStoredDataStream().ConfigureAwait(false);
    }

    /// <inheritdoc/>
    protected override ValueTask<Stream> GetDataStream()
    {
        return ValueTask.FromResult(_stream ?? throw new InvalidDataException("you need to provide a valid data stream to backup."));
    }
}

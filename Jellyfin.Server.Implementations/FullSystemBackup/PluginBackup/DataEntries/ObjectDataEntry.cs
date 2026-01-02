using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Jellyfin.Server.Implementations.FullSystemBackup.PluginBackup.DataEntries;

/// <summary>
/// Defines methods to store a c# object that is serializable in a backup.
/// </summary>
public class ObjectDataEntry : EagerSingleFileBackedDataEntryBase
{
    private readonly object? _objectData;
    private string? _objectJson;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectDataEntry"/> class.
    /// </summary>
    /// <param name="objectData">The object data to deserialise.</param>
    public ObjectDataEntry(object objectData)
    {
        _objectData = objectData;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ObjectDataEntry"/> class.
    /// </summary>
    public ObjectDataEntry()
    {
    }

    /// <summary>
    /// Reads the Backup value as a specific object.
    /// </summary>
    /// <typeparam name="T">The Object type .</typeparam>
    /// <returns>The deserialised object.</returns>
    public T? ReadAs<T>()
    {
        return JsonSerializer.Deserialize<T>(_objectJson ?? throw new InvalidDataException("No data available or not in read mode."));
    }

    /// <inheritdoc/>
    protected override async ValueTask<Stream> GetDataStream()
    {
        var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, _objectData).ConfigureAwait(false);
        return stream;
    }

    /// <inheritdoc/>
    protected override async ValueTask LoadFromDataStream(Stream dataStream)
    {
        using (var reader = new StreamReader(dataStream))
        {
            _objectJson = await reader.ReadToEndAsync().ConfigureAwait(false);
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;

namespace Emby.Server.Implementations.Serialization;

/// <summary>
/// Provides a wrapper around third party xml serialization.
/// </summary>
public class MyXmlSerializer : IXmlSerializer
{
    // Need to cache these
    // http://dotnetcodebox.blogspot.com/2013/01/xmlserializer-class-may-result-in.html
    private readonly ConcurrentDictionary<string, XmlSerializer> _serializers = new();

    private XmlSerializer GetSerializer(Type type)
        => _serializers.GetOrAdd(
            type.FullName ?? throw new ArgumentException($"Invalid type {type}."),
            static (_, t) => new XmlSerializer(t),
            type);

    /// <summary>
    /// Serializes to writer.
    /// </summary>
    /// <param name="obj">The obj.</param>
    /// <param name="writer">The writer.</param>
    private void SerializeToWriter(object obj, XmlWriter writer)
    {
        var netSerializer = GetSerializer(obj.GetType());
        netSerializer.Serialize(writer, obj);
    }

    /// <summary>
    /// Deserializes from stream.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="stream">The stream.</param>
    /// <returns>System.Object.</returns>
    public object? DeserializeFromStream(Type type, Stream stream)
    {
        using (var reader = XmlReader.Create(stream))
        {
            var netSerializer = GetSerializer(type);
            return netSerializer.Deserialize(reader);
        }
    }

    /// <summary>
    /// Serializes to stream.
    /// </summary>
    /// <param name="obj">The obj.</param>
    /// <param name="stream">The stream.</param>
    public void SerializeToStream(object obj, Stream stream)
    {
        using (var writer = new StreamWriter(stream, null, IODefaults.StreamWriterBufferSize, true))
        using (var textWriter = new XmlTextWriter(writer))
        {
            textWriter.Formatting = Formatting.Indented;
            SerializeToWriter(obj, textWriter);
        }
    }

    /// <summary>
    /// Serializes to file.
    /// </summary>
    /// <param name="obj">The obj.</param>
    /// <param name="file">The file.</param>
    public void SerializeToFile(object obj, string file)
    {
        using (var stream = new FileStream(file, FileMode.Create, FileAccess.Write))
        {
            SerializeToStream(obj, stream);
        }
    }

    /// <summary>
    /// Deserializes from file.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="file">The file.</param>
    /// <returns>System.Object.</returns>
    public object? DeserializeFromFile(Type type, string file)
    {
        using (var stream = File.OpenRead(file))
        {
            return DeserializeFromStream(type, stream);
        }
    }

    /// <summary>
    /// Deserializes from bytes.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="buffer">The buffer.</param>
    /// <returns>System.Object.</returns>
    public object? DeserializeFromBytes(Type type, byte[] buffer)
    {
        using (var stream = new MemoryStream(buffer, 0, buffer.Length, false, true))
        {
            return DeserializeFromStream(type, stream);
        }
    }
}

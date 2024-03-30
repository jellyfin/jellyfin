using System;
using System.Collections.Concurrent;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Serialization;

namespace Emby.Server.Implementations.Serialization
{
    /// <summary>
    /// Provides a wrapper around third party xml serialization.
    /// </summary>
    public class MyXmlSerializer : IXmlSerializer
    {
        // Need to cache these
        // http://dotnetcodebox.blogspot.com/2013/01/xmlserializer-class-may-result-in.html
        private static readonly ConcurrentDictionary<string, XmlSerializer> _serializers =
            new ConcurrentDictionary<string, XmlSerializer>();

        private static XmlSerializer GetSerializer(Type type)
            => _serializers.GetOrAdd(
                type.FullName ?? throw new ArgumentException($"Invalid type {type}."),
                static (_, t) => new XmlSerializer(t),
                type);

        private static void ThrowCollectibleException(Exception e)
        {
            var isCollectibleException = string.Equals(e.Message, "A non-collectible assembly may not reference a collectible assembly.", StringComparison.Ordinal);
            if (isCollectibleException)
            {
                throw new NotSupportedException($"It seems you are using the server's XML serializer in your plugin.\n" +
                                                $"This serializer is only used by the server to handle the plugin's config file " +
                                                $"and is not intended for the plugin to use directly.\n" +
                                                $"Please create your own XML serializer instance to handle plugin specific files.");
            }
        }

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
            try
            {
                using (var reader = XmlReader.Create(stream))
                {
                    var netSerializer = GetSerializer(type);
                    return netSerializer.Deserialize(reader);
                }
            }
            catch (NotSupportedException e)
            {
                ThrowCollectibleException(e);
                throw;
            }
        }

        /// <summary>
        /// Serializes to stream.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="stream">The stream.</param>
        public void SerializeToStream(object obj, Stream stream)
        {
            try
            {
                using (var writer = new StreamWriter(stream, null, IODefaults.StreamWriterBufferSize, true))
                using (var textWriter = new XmlTextWriter(writer))
                {
                    textWriter.Formatting = Formatting.Indented;
                    SerializeToWriter(obj, textWriter);
                }
            }
            catch (NotSupportedException e)
            {
                ThrowCollectibleException(e);
                throw;
            }
        }

        /// <summary>
        /// Serializes to file.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="file">The file.</param>
        public void SerializeToFile(object obj, string file)
        {
            try
            {
                using (var stream = new FileStream(file, FileMode.Create, FileAccess.Write))
                {
                    SerializeToStream(obj, stream);
                }
            }
            catch (NotSupportedException e)
            {
                ThrowCollectibleException(e);
                throw;
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
            try
            {
                using (var stream = File.OpenRead(file))
                {
                    return DeserializeFromStream(type, stream);
                }
            }
            catch (NotSupportedException e)
            {
                ThrowCollectibleException(e);
                throw;
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
            try
            {
                using (var stream = new MemoryStream(buffer, 0, buffer.Length, false, true))
                {
                    return DeserializeFromStream(type, stream);
                }
            }
            catch (NotSupportedException e)
            {
                ThrowCollectibleException(e);
                throw;
            }
        }
    }
}

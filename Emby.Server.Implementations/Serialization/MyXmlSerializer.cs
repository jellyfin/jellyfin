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
        // Keyed by the Type instance itself, not its name: plugins with the same type
        // name loaded side by side (e.g. two versions of a plugin, each in its own
        // PluginLoadContext) are distinct types, and sharing one serializer between
        // them fails at serialize time with an InvalidCastException across load contexts.
        private readonly ConcurrentDictionary<Type, XmlSerializer> _serializers = new();

        private XmlSerializer GetSerializer(Type type)
            => _serializers.GetOrAdd(type, static t => new XmlSerializer(t));

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
            // Serialize into a sibling temp file and swap it in, so a failure partway
            // through serialization (a throwing property getter, an incompatible type)
            // can never leave a truncated file where the configuration used to be.
            // Callers rely on this: BasePlugin.LoadConfiguration re-saves defaults over
            // an unreadable config, which is fine only because a failed save leaves the
            // previous bytes intact instead of a half-written document.
            // Note on SonarCloud S2083: this is not user input — `file` is a
            // server-internal path from the caller (e.g. BasePlugin.ConfigurationFilePath),
            // the same provenance as the direct FileStream(file) this replaces.
            var tempFile = file + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(tempFile, FileMode.Create, FileAccess.Write))
                {
                    SerializeToStream(obj, stream);
                }

                File.Move(tempFile, file, overwrite: true);
            }
            finally
            {
                try
                {
                    File.Delete(tempFile);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    // Best-effort cleanup of the temp file; the swap is what matters.
                }
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
            catch (Exception ex)
            {
                ex.Data.Add("Filename", file);
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
            using (var stream = new MemoryStream(buffer, 0, buffer.Length, false, true))
            {
                return DeserializeFromStream(type, stream);
            }
        }
    }
}

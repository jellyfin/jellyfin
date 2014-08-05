using MediaBrowser.Model.Serialization;
using System;
using System.IO;
using System.Xml;

namespace MediaBrowser.Common.Implementations.Serialization
{
    /// <summary>
    /// Provides a wrapper around third party xml serialization.
    /// </summary>
    public class XmlSerializer : IXmlSerializer
    {
        /// <summary>
        /// Serializes to writer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="writer">The writer.</param>
        private void SerializeToWriter(object obj, XmlTextWriter writer)
        {
            writer.Formatting = Formatting.Indented;
            var netSerializer = new System.Xml.Serialization.XmlSerializer(obj.GetType());
            netSerializer.Serialize(writer, obj);
        }

        /// <summary>
        /// Deserializes from stream.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="stream">The stream.</param>
        /// <returns>System.Object.</returns>
        public object DeserializeFromStream(Type type, Stream stream)
        {
            using (var reader = new XmlTextReader(stream))
            {
                var netSerializer = new System.Xml.Serialization.XmlSerializer(type);

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
            using (var writer = new XmlTextWriter(stream, null))
            {
                SerializeToWriter(obj, writer);
            }
        }

        /// <summary>
        /// Serializes to file.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="file">The file.</param>
        public void SerializeToFile(object obj, string file)
        {
            using (var stream = new FileStream(file, FileMode.Create))
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
        public object DeserializeFromFile(Type type, string file)
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
        public object DeserializeFromBytes(Type type, byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            {
                return DeserializeFromStream(type, stream);
            }
        }
    }
}

using MediaBrowser.Common.Logging;
using System;
using System.IO;
using System.Linq;
using System.Xml;

namespace MediaBrowser.Common.Serialization
{
    /// <summary>
    /// Provides a wrapper around third party xml serialization.
    /// </summary>
    public class XmlSerializer
    {
        /// <summary>
        /// Serializes to writer.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj">The obj.</param>
        /// <param name="writer">The writer.</param>
        public static void SerializeToWriter<T>(T obj, XmlTextWriter writer)
        {
            writer.Formatting = Formatting.Indented;
            var netSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            netSerializer.Serialize(writer, obj);
        }

        /// <summary>
        /// Serializes to writer.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="writer">The writer.</param>
        public static void SerializeToWriter(object obj, XmlTextWriter writer)
        {
            writer.Formatting = Formatting.Indented;
            var netSerializer = new System.Xml.Serialization.XmlSerializer(obj.GetType());
            netSerializer.Serialize(writer, obj);
        }

        /// <summary>
        /// Deserializes from stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream">The stream.</param>
        /// <returns>``0.</returns>
        public static T DeserializeFromStream<T>(Stream stream)
        {
            using (var reader = new XmlTextReader(stream))
            {
                var netSerializer = new System.Xml.Serialization.XmlSerializer(typeof(T));

                return (T)netSerializer.Deserialize(reader);
            }
        }

        /// <summary>
        /// Deserializes from stream.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="stream">The stream.</param>
        /// <returns>System.Object.</returns>
        public static object DeserializeFromStream(Type type, Stream stream)
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
        public static void SerializeToStream(object obj, Stream stream)
        {
            using (var writer = new XmlTextWriter(stream, null))
            {
                SerializeToWriter(obj, writer);
            }
        }

        /// <summary>
        /// Deserializes from file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="file">The file.</param>
        /// <returns>``0.</returns>
        public static T DeserializeFromFile<T>(string file)
        {
            using (var stream = File.OpenRead(file))
            {
                return DeserializeFromStream<T>(stream);
            }
        }

        /// <summary>
        /// Serializes to file.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="file">The file.</param>
        public static void SerializeToFile(object obj, string file)
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
        public static object DeserializeFromFile(Type type, string file)
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
        public static object DeserializeFromBytes(Type type, byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
            {
                return DeserializeFromStream(type, stream);
            }
        }

        /// <summary>
        /// Serializes to bytes.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.Byte[][].</returns>
        public static byte[] SerializeToBytes(object obj)
        {
            using (var stream = new MemoryStream())
            {
                SerializeToStream(obj, stream);

                return stream.ToArray();
            }
        }

        /// <summary>
        /// Reads an xml configuration file from the file system
        /// It will immediately re-serialize and save if new serialization data is available due to property changes
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="path">The path.</param>
        /// <returns>System.Object.</returns>
        public static object GetXmlConfiguration(Type type, string path)
        {
            Logger.LogInfo("Loading {0} at {1}", type.Name, path);

            object configuration;

            byte[] buffer = null;

            // Use try/catch to avoid the extra file system lookup using File.Exists
            try
            {
                buffer = File.ReadAllBytes(path);

                configuration = DeserializeFromBytes(type, buffer);
            }
            catch (FileNotFoundException)
            {
                configuration = Activator.CreateInstance(type);
            }

            // Take the object we just got and serialize it back to bytes
            var newBytes = SerializeToBytes(configuration);

            // If the file didn't exist before, or if something has changed, re-save
            if (buffer == null || !buffer.SequenceEqual(newBytes))
            {
                Logger.LogInfo("Saving {0} to {1}", type.Name, path);

                // Save it after load in case we got new items
                File.WriteAllBytes(path, newBytes);
            }

            return configuration;
        }

        /// <summary>
        /// Reads an xml configuration file from the file system
        /// It will immediately save the configuration after loading it, just
        /// in case there are new serializable properties
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path">The path.</param>
        /// <returns>``0.</returns>
        public static T GetXmlConfiguration<T>(string path)
            where T : class
        {
            return GetXmlConfiguration(typeof(T), path) as T;
        }
    }
}

using System;
using System.IO;

namespace MediaBrowser.Common.Serialization
{
    /// <summary>
    /// Provides a wrapper around third party xml serialization.
    /// </summary>
    public class XmlSerializer
    {
        public static void SerializeToStream<T>(T obj, Stream stream)
        {
            ServiceStack.Text.XmlSerializer.SerializeToStream(obj, stream);
        }

        public static void SerializeToFile<T>(T obj, string file)
        {
            using (StreamWriter streamWriter = new StreamWriter(file))
            {
                ServiceStack.Text.XmlSerializer.SerializeToWriter<T>(obj, streamWriter);
            }
        }

        public static object DeserializeFromFile(Type type, string file)
        {
            using (Stream stream = File.OpenRead(file))
            {
                return ServiceStack.Text.XmlSerializer.DeserializeFromStream(type, stream);
            }
        }

        public static T DeserializeFromFile<T>(string file)
        {
            using (Stream stream = File.OpenRead(file))
            {
                return ServiceStack.Text.XmlSerializer.DeserializeFromStream<T>(stream);
            }
        }

        public static T DeserializeFromStream<T>(Stream stream)
        {
            return ServiceStack.Text.XmlSerializer.DeserializeFromStream<T>(stream);
        }

        public static T DeserializeFromString<T>(string data)
        {
            return ServiceStack.Text.XmlSerializer.DeserializeFromString<T>(data);
        }
    }
}

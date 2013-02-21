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

        public static T DeserializeFromStream<T>(Stream stream)
        {
            return ServiceStack.Text.XmlSerializer.DeserializeFromStream<T>(stream);
        }

        public static object DeserializeFromStream(Type type, Stream stream)
        {
            return ServiceStack.Text.XmlSerializer.DeserializeFromStream(type, stream);
        }

        public static void SerializeToFile<T>(T obj, string file)
        {
            using (var stream = new FileStream(file, FileMode.Create))
            {
                SerializeToStream(obj, stream);
            }
        }

        public static T DeserializeFromFile<T>(string file)
        {
            using (Stream stream = File.OpenRead(file))
            {
                return DeserializeFromStream<T>(stream);
            }
        }

        public static void SerializeToFile(object obj, string file)
        {
            using (var stream = new FileStream(file, FileMode.Create))
            {
                ServiceStack.Text.XmlSerializer.SerializeToStream(obj, stream);
            }
        }
        
        public static object DeserializeFromFile(Type type, string file)
        {
            using (Stream stream = File.OpenRead(file))
            {
                return DeserializeFromStream(type, stream);
            }
        }
    }
}

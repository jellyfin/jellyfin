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
            GetSerializer(typeof(T)).Serialize(stream, obj);
        }

        public static void SerializeToFile<T>(T obj, string file)
        {
            using (FileStream stream = new FileStream(file, FileMode.Create))
            {
                GetSerializer(typeof(T)).Serialize(stream, obj);
            }
        }

        public static T DeserializeFromFile<T>(string file)
        {
            using (Stream stream = File.OpenRead(file))
            {
                return (T)GetSerializer(typeof(T)).Deserialize(stream);
            }
        }

        public static object DeserializeFromFile(Type type, string file)
        {
            using (Stream stream = File.OpenRead(file))
            {
                return GetSerializer(type).Deserialize(stream);
            }
        }

        public static T DeserializeFromStream<T>(Stream stream)
        {
            return (T)GetSerializer(typeof(T)).Deserialize(stream);
        }

        private static System.Xml.Serialization.XmlSerializer GetSerializer(Type type)
        {
            return new System.Xml.Serialization.XmlSerializer(type);
        }
    }
}

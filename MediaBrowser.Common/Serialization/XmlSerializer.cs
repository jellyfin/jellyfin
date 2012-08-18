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
            GetSerializer<T>().Serialize(stream, obj);
        }

        public static void SerializeToFile<T>(T obj, string file)
        {
            using (FileStream stream = new FileStream(file, FileMode.Create))
            {
                GetSerializer<T>().Serialize(stream, obj);
            }
        }

        public static T DeserializeFromFile<T>(string file)
        {
            using (Stream stream = File.OpenRead(file))
            {
                return (T)GetSerializer<T>().Deserialize(stream);
            }
        }

        public static T DeserializeFromStream<T>(Stream stream)
        {
            return (T)GetSerializer<T>().Deserialize(stream);
        }

        private static System.Xml.Serialization.XmlSerializer GetSerializer<T>()
        {
            return new System.Xml.Serialization.XmlSerializer(typeof(T));
        }
    }
}

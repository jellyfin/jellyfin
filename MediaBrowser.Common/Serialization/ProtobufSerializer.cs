using System.IO;

namespace MediaBrowser.Common.Serialization
{
    /// <summary>
    /// Protocol buffers is google's binary serialization format. This is a .NET implementation of it.
    /// You have to tag your classes with some annoying attributes, but in return you get the fastest serialization around with the smallest possible output.
    /// </summary>
    public static class ProtobufSerializer
    {
        public static void SerializeToStream<T>(T obj, Stream stream)
        {
            ProtoBuf.Serializer.Serialize<T>(stream, obj);
        }

        public static T DeserializeFromStream<T>(Stream stream)
        {
            return ProtoBuf.Serializer.Deserialize<T>(stream);
        }

        public static void SerializeToFile<T>(T obj, string file)
        {
            using (Stream stream = File.Open(file, FileMode.Create))
            {
                SerializeToStream<T>(obj, stream);
            }
        }

        public static T DeserializeFromFile<T>(string file)
        {
            using (Stream stream = File.OpenRead(file))
            {
                return DeserializeFromStream<T>(stream);
            }
        }
    }
}

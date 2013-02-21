using System;
using System.IO;

namespace MediaBrowser.Common.Serialization
{
    /// <summary>
    /// Protocol buffers is google's binary serialization format. This is a .NET implementation of it.
    /// You have to tag your classes with some annoying attributes, but in return you get the fastest serialization around with the smallest possible output.
    /// </summary>
    public static class ProtobufSerializer
    {
        /// <summary>
        /// This is an auto-generated Protobuf Serialization assembly for best performance.
        /// It is created during the Model project's post-build event.
        /// This means that this class can currently only handle types within the Model project.
        /// If we need to, we can always add a param indicating whether or not the model serializer should be used.
        /// </summary>
        private static readonly ProtobufModelSerializer ProtobufModelSerializer = new ProtobufModelSerializer();

        public static void SerializeToStream<T>(T obj, Stream stream)
        {
            ProtobufModelSerializer.Serialize(stream, obj);
        }

        public static T DeserializeFromStream<T>(Stream stream)
            where T : class
        {
            return ProtobufModelSerializer.Deserialize(stream, null, typeof(T)) as T;
        }

        public static object DeserializeFromStream(Stream stream, Type type)
        {
            return ProtobufModelSerializer.Deserialize(stream, null, type);
        }

        public static void SerializeToFile<T>(T obj, string file)
        {
            using (Stream stream = File.Open(file, FileMode.Create))
            {
                SerializeToStream(obj, stream);
            }
        }

        public static T DeserializeFromFile<T>(string file)
            where T : class
        {
            using (Stream stream = File.OpenRead(file))
            {
                return DeserializeFromStream<T>(stream);
            }
        }
    }
}

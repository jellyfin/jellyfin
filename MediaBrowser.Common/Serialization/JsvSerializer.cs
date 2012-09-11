using System;
using System.IO;

namespace MediaBrowser.Common.Serialization
{
    /// <summary>
    /// This adds support for ServiceStack's proprietary JSV output format.
    /// It's a hybrid of Json and Csv but the serializer performs about 25% faster and output runs about 10% smaller
    /// http://www.servicestack.net/benchmarks/NorthwindDatabaseRowsSerialization.100000-times.2010-08-17.html
    /// </summary>
    public static class JsvSerializer
    {
        public static void SerializeToStream<T>(T obj, Stream stream)
        {
            ServiceStack.Text.TypeSerializer.SerializeToStream(obj, stream);
        }

        public static T DeserializeFromStream<T>(Stream stream)
        {
            return ServiceStack.Text.TypeSerializer.DeserializeFromStream<T>(stream);
        }

        public static object DeserializeFromStream(Stream stream, Type type)
        {
            return ServiceStack.Text.TypeSerializer.DeserializeFromStream(type, stream);
        }

        public static void SerializeToFile<T>(T obj, string file)
        {
            using (Stream stream = File.Open(file, FileMode.Create))
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
    }
}

using System.IO;

namespace MediaBrowser.Common.Serialization
{
    /// <summary>
    /// This adds support for ServiceStack's proprietary JSV output format.
    /// It's based on Json but the serializer performs faster and output runs about 10% smaller
    /// http://www.servicestack.net/benchmarks/NorthwindDatabaseRowsSerialization.100000-times.2010-08-17.html
    /// </summary>
    public static class JsvSerializer
    {
        public static void SerializeToStream<T>(T obj, Stream stream)
        {
            ServiceStack.Text.TypeSerializer.SerializeToStream<T>(obj, stream);
        }

        public static T DeserializeFromStream<T>(Stream stream)
        {
            return ServiceStack.Text.TypeSerializer.DeserializeFromStream<T>(stream);
        }
    }
}

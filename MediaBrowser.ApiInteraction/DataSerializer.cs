using System;
using System.IO;
using ProtoBuf;
using ServiceStack.Text;

namespace MediaBrowser.ApiInteraction
{
    public static class DataSerializer
    {
        public static T DeserializeJsonFromStream<T>(Stream stream)
        {
            return JsonSerializer.DeserializeFromStream<T>(stream);
        }

        public static T DeserializeJsvFromStream<T>(Stream stream)
        {
            return TypeSerializer.DeserializeFromStream<T>(stream);
        }

        public static object DeserializeJsvFromStream(Stream stream, Type type)
        {
            return TypeSerializer.DeserializeFromStream(type, stream);
        }

        public static object DeserializeJsonFromStream(Stream stream, Type type)
        {
            return JsonSerializer.DeserializeFromStream(type, stream);
        }
        
        public static T DeserializeProtobufFromStream<T>(Stream stream)
        {
            return Serializer.Deserialize<T>(stream);
        }

        public static void Configure()
        {
            JsConfig.DateHandler = ServiceStack.Text.JsonDateHandler.ISO8601;
            JsConfig.ExcludeTypeInfo = true;
            JsConfig.IncludeNullValues = false;
        }
    }

    public enum SerializationFormat
    {
        Json,
        Jsv,
        Protobuf
    }
}

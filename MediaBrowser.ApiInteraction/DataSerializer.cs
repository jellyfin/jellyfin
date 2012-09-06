using System;
using System.IO;
using ProtoBuf;
using ServiceStack.Text;

namespace MediaBrowser.ApiInteraction
{
    public static class DataSerializer
    {
        public static T DeserializeFromStream<T>(Stream stream, SerializationFormats format)
        {
            if (format == ApiInteraction.SerializationFormats.Protobuf)
            {
                return Serializer.Deserialize<T>(stream);
            }
            if (format == ApiInteraction.SerializationFormats.Jsv)
            {
                return TypeSerializer.DeserializeFromStream<T>(stream);
            }

            return JsonSerializer.DeserializeFromStream<T>(stream);
        }

        public static object DeserializeFromStream(Stream stream, SerializationFormats format, Type type)
        {
            if (format == ApiInteraction.SerializationFormats.Protobuf)
            {
                throw new NotImplementedException();
            }
            if (format == ApiInteraction.SerializationFormats.Jsv)
            {
                return TypeSerializer.DeserializeFromStream(type, stream);
            }

            return JsonSerializer.DeserializeFromStream(type, stream);
        }

        public static void Configure()
        {
            JsConfig.DateHandler = ServiceStack.Text.JsonDateHandler.ISO8601;
            JsConfig.ExcludeTypeInfo = true;
            JsConfig.IncludeNullValues = false;
        }

        public static bool CanDeSerializeJsv
        {
            get
            {
                return true;
            }
        }
    }
}

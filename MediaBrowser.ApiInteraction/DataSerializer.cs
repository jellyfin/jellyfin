using ServiceStack.Text;
using System;
using System.IO;

namespace MediaBrowser.ApiInteraction
{
    public static class DataSerializer
    {
        /// <summary>
        /// This is an auto-generated Protobuf Serialization assembly for best performance.
        /// It is created during the Model project's post-build event.
        /// This means that this class can currently only handle types within the Model project.
        /// If we need to, we can always add a param indicating whether or not the model serializer should be used.
        /// </summary>
        private static readonly ProtobufModelSerializer ProtobufModelSerializer = new ProtobufModelSerializer();
        
        /// <summary>
        /// Deserializes an object using generics
        /// </summary>
        public static T DeserializeFromStream<T>(Stream stream, SerializationFormats format)
            where T : class
        {
            if (format == SerializationFormats.Protobuf)
            {
                //return Serializer.Deserialize<T>(stream);
                return ProtobufModelSerializer.Deserialize(stream, null, typeof(T)) as T;
            }
            if (format == SerializationFormats.Jsv)
            {
                return TypeSerializer.DeserializeFromStream<T>(stream);
            }
            if (format == SerializationFormats.Json)
            {
                return JsonSerializer.DeserializeFromStream<T>(stream);
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Deserializes an object
        /// </summary>
        public static object DeserializeFromStream(Stream stream, SerializationFormats format, Type type)
        {
            if (format == SerializationFormats.Protobuf)
            {
                //throw new NotImplementedException();
                return ProtobufModelSerializer.Deserialize(stream, null, type);
            }
            if (format == SerializationFormats.Jsv)
            {
                return TypeSerializer.DeserializeFromStream(type, stream);
            }
            if (format == SerializationFormats.Json)
            {
                return JsonSerializer.DeserializeFromStream(type, stream);
            }

            throw new NotImplementedException();
        }

        public static void Configure()
        {
            JsConfig.DateHandler = JsonDateHandler.ISO8601;
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

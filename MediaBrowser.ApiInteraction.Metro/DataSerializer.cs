using Newtonsoft.Json;
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
        
        public static T DeserializeFromStream<T>(Stream stream, SerializationFormats format)
            where T : class
        {
            if (format == ApiInteraction.SerializationFormats.Protobuf)
            {
                return ProtobufModelSerializer.Deserialize(stream, null, typeof(T)) as T;
            }
            else if (format == ApiInteraction.SerializationFormats.Jsv)
            {
                throw new NotImplementedException();
            }
            else if (format == ApiInteraction.SerializationFormats.Json)
            {
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    using (JsonReader jsonReader = new JsonTextReader(streamReader))
                    {
                        return JsonSerializer.Create(new JsonSerializerSettings()).Deserialize<T>(jsonReader);
                    }
                }
            }

            throw new NotImplementedException();
        }

        public static object DeserializeFromStream(Stream stream, SerializationFormats format, Type type)
        {
            if (format == ApiInteraction.SerializationFormats.Protobuf)
            {
                return ProtobufModelSerializer.Deserialize(stream, null, type);
            }
            else if (format == ApiInteraction.SerializationFormats.Jsv)
            {
                throw new NotImplementedException();
            }
            else if (format == ApiInteraction.SerializationFormats.Json)
            {
                using (StreamReader streamReader = new StreamReader(stream))
                {
                    using (JsonReader jsonReader = new JsonTextReader(streamReader))
                    {
                        return JsonSerializer.Create(new JsonSerializerSettings()).Deserialize(jsonReader, type);
                    }
                }
            }

            throw new NotImplementedException();
        }

        public static void Configure()
        {
        }

        public static bool CanDeSerializeJsv
        {
            get
            {
                return false;
            }
        }
    }
}

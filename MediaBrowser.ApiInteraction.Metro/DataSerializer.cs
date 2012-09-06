using System;
using System.IO;
using Newtonsoft.Json;
using ProtoBuf;

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
                throw new NotImplementedException();
            }

            using (StreamReader streamReader = new StreamReader(stream))
            {
                using (JsonReader jsonReader = new JsonTextReader(streamReader))
                {
                    return JsonSerializer.Create(new JsonSerializerSettings()).Deserialize<T>(jsonReader);
                }
            }
        }

        public static object DeserializeFromStream(Stream stream, SerializationFormats format, Type type)
        {
            if (format == ApiInteraction.SerializationFormats.Protobuf)
            {
                throw new NotImplementedException();
            }
            if (format == ApiInteraction.SerializationFormats.Jsv)
            {
                throw new NotImplementedException();
            }

            using (StreamReader streamReader = new StreamReader(stream))
            {
                using (JsonReader jsonReader = new JsonTextReader(streamReader))
                {
                    return JsonSerializer.Create(new JsonSerializerSettings()).Deserialize(jsonReader, type);
                }
            }
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

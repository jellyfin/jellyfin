using System;
using System.IO;

namespace MediaBrowser.Common.Json
{
    public class JsonSerializer
    {
        public static void SerializeToStream<T>(T obj, Stream stream)
        {
            ServiceStack.Text.JsonSerializer.SerializeToStream<T>(obj, stream);
        }

        public static void SerializeToFile<T>(T obj, string file)
        {
            using (StreamWriter streamWriter = new StreamWriter(file))
            {
                ServiceStack.Text.JsonSerializer.SerializeToWriter<T>(obj, streamWriter);
            }
        }

        public static object DeserializeFromFile(Type type, string file)
        {
            using (Stream stream = File.OpenRead(file))
            {
                return ServiceStack.Text.JsonSerializer.DeserializeFromStream(type, stream);
            }
        }

        public static T DeserializeFromFile<T>(string file)
        {
            using (Stream stream = File.OpenRead(file))
            {
                return ServiceStack.Text.JsonSerializer.DeserializeFromStream<T>(stream);
            }
        }

        public static T DeserializeFromStream<T>(Stream stream)
        {
            return ServiceStack.Text.JsonSerializer.DeserializeFromStream<T>(stream);
        }

        public static T DeserializeFromString<T>(string data)
        {
            return ServiceStack.Text.JsonSerializer.DeserializeFromString<T>(data);
        }

        public static void Configure()
        {
            ServiceStack.Text.JsConfig.ExcludeTypeInfo = true;
            ServiceStack.Text.JsConfig.IncludeNullValues = false;
        }
    }
}

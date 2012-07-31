using System;
using System.IO;

namespace MediaBrowser.Common.Serialization
{
    /// <summary>
    /// Provides a wrapper around third party json serialization.
    /// </summary>
    public class JsonSerializer
    {
        public static void SerializeToStream<T>(T obj, Stream stream)
        {
            Configure();

            ServiceStack.Text.JsonSerializer.SerializeToStream<T>(obj, stream);
        }

        public static void SerializeToFile<T>(T obj, string file)
        {
            Configure();

            using (StreamWriter streamWriter = new StreamWriter(file))
            {
                ServiceStack.Text.JsonSerializer.SerializeToWriter<T>(obj, streamWriter);
            }
        }

        public static object DeserializeFromFile(Type type, string file)
        {
            Configure();

            using (Stream stream = File.OpenRead(file))
            {
                return ServiceStack.Text.JsonSerializer.DeserializeFromStream(type, stream);
            }
        }

        public static T DeserializeFromFile<T>(string file)
        {
            Configure();

            using (Stream stream = File.OpenRead(file))
            {
                return ServiceStack.Text.JsonSerializer.DeserializeFromStream<T>(stream);
            }
        }

        public static T DeserializeFromStream<T>(Stream stream)
        {
            Configure();

            return ServiceStack.Text.JsonSerializer.DeserializeFromStream<T>(stream);
        }

        public static T DeserializeFromString<T>(string data)
        {
            Configure();

            return ServiceStack.Text.JsonSerializer.DeserializeFromString<T>(data);
        }

        private static bool IsConfigured = false;
        private static void Configure()
        {
            if (!IsConfigured)
            {
                ServiceStack.Text.JsConfig.ExcludeTypeInfo = true;
                ServiceStack.Text.JsConfig.IncludeNullValues = false;

                IsConfigured = true;
            }
        }
    }
}

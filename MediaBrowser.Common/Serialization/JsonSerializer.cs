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

            ServiceStack.Text.JsonSerializer.SerializeToStream(obj, stream);
        }

        public static void SerializeToFile<T>(T obj, string file)
        {
            Configure();

            using (Stream stream = File.Open(file, FileMode.Create))
            {
                ServiceStack.Text.JsonSerializer.SerializeToStream(obj, stream);
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

        public static object DeserializeFromStream(Stream stream, Type type)
        {
            Configure();

            return ServiceStack.Text.JsonSerializer.DeserializeFromStream(type, stream);
        }

        private static bool _isConfigured;
        private static void Configure()
        {
            if (!_isConfigured)
            {
                ServiceStack.Text.JsConfig.DateHandler = ServiceStack.Text.JsonDateHandler.ISO8601;
                ServiceStack.Text.JsConfig.ExcludeTypeInfo = true;
                ServiceStack.Text.JsConfig.IncludeNullValues = false;
                _isConfigured = true;
            }
        }
    }
}

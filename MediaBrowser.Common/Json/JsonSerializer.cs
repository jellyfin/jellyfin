using System.IO;

namespace MediaBrowser.Common.Json
{
    public class JsonSerializer
    {
        public static void SerializeToStream<T>(T o, Stream stream)
        {
            Configure();
            
            ServiceStack.Text.JsonSerializer.SerializeToStream<T>(o, stream);
        }

        public static void SerializeToFile<T>(T o, string file)
        {
            Configure();

            using (StreamWriter streamWriter = new StreamWriter(file))
            {
                ServiceStack.Text.JsonSerializer.SerializeToWriter<T>(o, streamWriter);
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
        
        private static void Configure()
        {
            ServiceStack.Text.JsConfig.ExcludeTypeInfo = true;
            ServiceStack.Text.JsConfig.IncludeNullValues = false;
            ServiceStack.Text.JsConfig.DateHandler = ServiceStack.Text.JsonDateHandler.ISO8601;
        }
    }
}

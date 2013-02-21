using ProtoBuf;
using ProtoBuf.Meta;
using ServiceStack.Text;
using System;
using System.IO;

namespace MediaBrowser.ApiInteraction
{
    /// <summary>
    /// Class DataSerializer
    /// </summary>
    public static class DataSerializer
    {
        /// <summary>
        /// Gets or sets the dynamically created serializer.
        /// </summary>
        /// <value>The dynamic serializer.</value>
        public static TypeModel DynamicSerializer { get; set; }

        /// <summary>
        /// Deserializes an object
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="format">The format.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.NotImplementedException"></exception>
        public static object DeserializeFromStream(Stream stream, SerializationFormats format, Type type)
        {
            if (format == SerializationFormats.Protobuf)
            {
                if (DynamicSerializer != null)
                {
                    return DynamicSerializer.Deserialize(stream, null, type);
                }
                return Serializer.NonGeneric.Deserialize(type, stream);
            }
            if (format == SerializationFormats.Json)
            {
                return JsonSerializer.DeserializeFromStream(type, stream);
            }

            throw new NotImplementedException();
        }

        /// <summary>
        /// Serializes to json.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.String.</returns>
        public static string SerializeToJsonString(object obj)
        {
            return JsonSerializer.SerializeToString(obj, obj.GetType());
        }

        /// <summary>
        /// Configures this instance.
        /// </summary>
        public static void Configure()
        {
            JsConfig.DateHandler = JsonDateHandler.ISO8601;
            JsConfig.ExcludeTypeInfo = true;
            JsConfig.IncludeNullValues = false;
        }
    }
}

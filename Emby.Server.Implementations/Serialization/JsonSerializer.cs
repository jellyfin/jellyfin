#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Model.Serialization;

namespace Emby.Server.Implementations.Serialization
{
    /// <summary>
    /// Provides a wrapper around third party json serialization.
    /// </summary>
    public class JsonSerializer : IJsonSerializer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonSerializer" /> class.
        /// </summary>
        public JsonSerializer()
        {
            ServiceStack.Text.JsConfig.DateHandler = ServiceStack.Text.DateHandler.ISO8601;
            ServiceStack.Text.JsConfig.ExcludeTypeInfo = true;
            ServiceStack.Text.JsConfig.IncludeNullValues = false;
            ServiceStack.Text.JsConfig.AlwaysUseUtc = true;
            ServiceStack.Text.JsConfig.AssumeUtc = true;

            ServiceStack.Text.JsConfig<Guid>.SerializeFn = SerializeGuid;
        }

        /// <summary>
        /// Serializes to stream.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="stream">The stream.</param>
        /// <exception cref="ArgumentNullException">obj</exception>
        public void SerializeToStream(object obj, Stream stream)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            ServiceStack.Text.JsonSerializer.SerializeToStream(obj, obj.GetType(), stream);
        }

        /// <summary>
        /// Serializes to stream.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="stream">The stream.</param>
        /// <exception cref="ArgumentNullException">obj</exception>
        public void SerializeToStream<T>(T obj, Stream stream)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            ServiceStack.Text.JsonSerializer.SerializeToStream<T>(obj, stream);
        }

        /// <summary>
        /// Serializes to file.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="file">The file.</param>
        /// <exception cref="ArgumentNullException">obj</exception>
        public void SerializeToFile(object obj, string file)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException(nameof(file));
            }

            using (var stream = new FileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                SerializeToStream(obj, stream);
            }
        }

        private static Stream OpenFile(string path)
        {
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 131072);
        }

        /// <summary>
        /// Deserializes from file.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="file">The file.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="ArgumentNullException">type</exception>
        public object DeserializeFromFile(Type type, string file)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException(nameof(file));
            }

            using (var stream = OpenFile(file))
            {
                return DeserializeFromStream(stream, type);
            }
        }

        /// <summary>
        /// Deserializes from file.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="file">The file.</param>
        /// <returns>``0.</returns>
        /// <exception cref="ArgumentNullException">file</exception>
        public T DeserializeFromFile<T>(string file)
            where T : class
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException(nameof(file));
            }

            using (var stream = OpenFile(file))
            {
                return DeserializeFromStream<T>(stream);
            }
        }

        /// <summary>
        /// Deserializes from stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream">The stream.</param>
        /// <returns>``0.</returns>
        /// <exception cref="ArgumentNullException">stream</exception>
        public T DeserializeFromStream<T>(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return ServiceStack.Text.JsonSerializer.DeserializeFromStream<T>(stream);
        }

        public Task<T> DeserializeFromStreamAsync<T>(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            return ServiceStack.Text.JsonSerializer.DeserializeFromStreamAsync<T>(stream);
        }

        /// <summary>
        /// Deserializes from string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text">The text.</param>
        /// <returns>``0.</returns>
        /// <exception cref="ArgumentNullException">text</exception>
        public T DeserializeFromString<T>(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException(nameof(text));
            }

            return ServiceStack.Text.JsonSerializer.DeserializeFromString<T>(text);
        }

        /// <summary>
        /// Deserializes from stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="ArgumentNullException">stream</exception>
        public object DeserializeFromStream(Stream stream, Type type)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return ServiceStack.Text.JsonSerializer.DeserializeFromStream(type, stream);
        }

        public async Task<object> DeserializeFromStreamAsync(Stream stream, Type type)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            using (var reader = new StreamReader(stream))
            {
                var json = await reader.ReadToEndAsync().ConfigureAwait(false);

                return ServiceStack.Text.JsonSerializer.DeserializeFromString(json, type);
            }
        }

        private static string SerializeGuid(Guid guid)
        {
            if (guid.Equals(Guid.Empty))
            {
                return null;
            }

            return guid.ToString("N", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Deserializes from string.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="ArgumentNullException">json</exception>
        public object DeserializeFromString(string json, Type type)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentNullException(nameof(json));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            return ServiceStack.Text.JsonSerializer.DeserializeFromString(json, type);
        }

        /// <summary>
        /// Serializes to string.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="ArgumentNullException">obj</exception>
        public string SerializeToString(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            return ServiceStack.Text.JsonSerializer.SerializeToString(obj, obj.GetType());
        }
    }
}

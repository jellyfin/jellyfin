using MediaBrowser.Model.Serialization;
using System;
using System.IO;
using CommonIO;
using MediaBrowser.Model.Logging;

namespace MediaBrowser.Common.Implementations.Serialization
{
    /// <summary>
    /// Provides a wrapper around third party json serialization.
    /// </summary>
    public class JsonSerializer : IJsonSerializer
    {
        private readonly IFileSystem _fileSystem;
        private readonly ILogger _logger;

        public JsonSerializer(IFileSystem fileSystem, ILogger logger)
        {
            _fileSystem = fileSystem;
            _logger = logger;
            Configure();
        }

        /// <summary>
        /// Serializes to stream.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="stream">The stream.</param>
        /// <exception cref="System.ArgumentNullException">obj</exception>
        public void SerializeToStream(object obj, Stream stream)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            ServiceStack.Text.JsonSerializer.SerializeToStream(obj, obj.GetType(), stream);
        }

        /// <summary>
        /// Serializes to file.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <param name="file">The file.</param>
        /// <exception cref="System.ArgumentNullException">obj</exception>
        public void SerializeToFile(object obj, string file)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("file");
            }

			using (Stream stream = _fileSystem.GetFileStream(file, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                SerializeToStream(obj, stream);
            }
        }

        private Stream OpenFile(string path)
        {
            _logger.Debug("Deserializing file {0}", path);
            return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 131072);
        }

        /// <summary>
        /// Deserializes from file.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="file">The file.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.ArgumentNullException">type</exception>
        public object DeserializeFromFile(Type type, string file)
        {
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("file");
            }

            using (Stream stream = OpenFile(file))
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
        /// <exception cref="System.ArgumentNullException">file</exception>
        public T DeserializeFromFile<T>(string file)
            where T : class
        {
            if (string.IsNullOrEmpty(file))
            {
                throw new ArgumentNullException("file");
            }

            using (Stream stream = OpenFile(file))
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
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public T DeserializeFromStream<T>(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            return ServiceStack.Text.JsonSerializer.DeserializeFromStream<T>(stream);
        }

        /// <summary>
        /// Deserializes from string.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="text">The text.</param>
        /// <returns>``0.</returns>
        /// <exception cref="System.ArgumentNullException">text</exception>
        public T DeserializeFromString<T>(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                throw new ArgumentNullException("text");
            }

            return ServiceStack.Text.JsonSerializer.DeserializeFromString<T>(text);
        }

        /// <summary>
        /// Deserializes from stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.ArgumentNullException">stream</exception>
        public object DeserializeFromStream(Stream stream, Type type)
        {
            if (stream == null)
            {
                throw new ArgumentNullException("stream");
            }

            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            return ServiceStack.Text.JsonSerializer.DeserializeFromStream(type, stream);
        }

        /// <summary>
        /// Configures this instance.
        /// </summary>
        private void Configure()
        {
            ServiceStack.Text.JsConfig.DateHandler = ServiceStack.Text.DateHandler.ISO8601;
            ServiceStack.Text.JsConfig.ExcludeTypeInfo = true;
            ServiceStack.Text.JsConfig.IncludeNullValues = false;
            ServiceStack.Text.JsConfig.AlwaysUseUtc = true;
            ServiceStack.Text.JsConfig.AssumeUtc = true;
        }

        /// <summary>
        /// Deserializes from string.
        /// </summary>
        /// <param name="json">The json.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.Object.</returns>
        /// <exception cref="System.ArgumentNullException">json</exception>
        public object DeserializeFromString(string json, Type type)
        {
            if (string.IsNullOrEmpty(json))
            {
                throw new ArgumentNullException("json");
            }

            if (type == null)
            {
                throw new ArgumentNullException("type");
            }

            return ServiceStack.Text.JsonSerializer.DeserializeFromString(json, type);
        }

        /// <summary>
        /// Serializes to string.
        /// </summary>
        /// <param name="obj">The obj.</param>
        /// <returns>System.String.</returns>
        /// <exception cref="System.ArgumentNullException">obj</exception>
        public string SerializeToString(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return ServiceStack.Text.JsonSerializer.SerializeToString(obj, obj.GetType());
        }
    }
}

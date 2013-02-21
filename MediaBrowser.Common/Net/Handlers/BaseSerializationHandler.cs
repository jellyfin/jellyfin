using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Serialization;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net.Handlers
{
    /// <summary>
    /// Class BaseSerializationHandler
    /// </summary>
    /// <typeparam name="TKernelType">The type of the T kernel type.</typeparam>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseSerializationHandler<TKernelType, T> : BaseHandler<TKernelType>
        where TKernelType : IKernel
        where T : class
    {
        /// <summary>
        /// Gets the serialization format.
        /// </summary>
        /// <value>The serialization format.</value>
        public SerializationFormat SerializationFormat
        {
            get
            {
                var format = QueryString["dataformat"];

                if (string.IsNullOrEmpty(format))
                {
                    return SerializationFormat.Json;
                }

                return (SerializationFormat)Enum.Parse(typeof(SerializationFormat), format, true);
            }
        }

        /// <summary>
        /// Gets the type of the content.
        /// </summary>
        /// <value>The type of the content.</value>
        protected string ContentType
        {
            get
            {
                switch (SerializationFormat)
                {
                    case SerializationFormat.Protobuf:
                        return "application/x-protobuf";
                    default:
                        return MimeTypes.JsonMimeType;
                }
            }
        }

        /// <summary>
        /// Gets the response info.
        /// </summary>
        /// <returns>Task{ResponseInfo}.</returns>
        protected override Task<ResponseInfo> GetResponseInfo()
        {
            return Task.FromResult(new ResponseInfo
            {
                ContentType = ContentType
            });
        }

        /// <summary>
        /// Called when [processing request].
        /// </summary>
        /// <param name="responseInfo">The response info.</param>
        /// <returns>Task.</returns>
        protected override async Task OnProcessingRequest(ResponseInfo responseInfo)
        {
            _objectToSerialize = await GetObjectToSerialize().ConfigureAwait(false);

            if (_objectToSerialize == null)
            {
                throw new ResourceNotFoundException();
            }

            await base.OnProcessingRequest(responseInfo).ConfigureAwait(false);
        }

        /// <summary>
        /// The _object to serialize
        /// </summary>
        private T _objectToSerialize;

        /// <summary>
        /// Gets the object to serialize.
        /// </summary>
        /// <returns>Task{`0}.</returns>
        protected abstract Task<T> GetObjectToSerialize();

        /// <summary>
        /// Writes the response to output stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="responseInfo">The response info.</param>
        /// <param name="contentLength">Length of the content.</param>
        /// <returns>Task.</returns>
        protected override Task WriteResponseToOutputStream(Stream stream, ResponseInfo responseInfo, long? contentLength)
        {
            return Task.Run(() =>
            {
                switch (SerializationFormat)
                {
                    case SerializationFormat.Protobuf:
                        Kernel.ProtobufSerializer.SerializeToStream(_objectToSerialize, stream);
                        break;
                    default:
                        JsonSerializer.SerializeToStream(_objectToSerialize, stream);
                        break;
                }
            });
        }
    }

    /// <summary>
    /// Enum SerializationFormat
    /// </summary>
    public enum SerializationFormat
    {
        /// <summary>
        /// The json
        /// </summary>
        Json,
        /// <summary>
        /// The protobuf
        /// </summary>
        Protobuf
    }
}

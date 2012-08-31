using System;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Common.Serialization;

namespace MediaBrowser.Common.Net.Handlers
{
    public abstract class BaseJsonHandler<T> : BaseHandler
    {
        public SerializationFormat SerializationFormat
        {
            get
            {
                string format = QueryString["dataformat"];

                if (string.IsNullOrEmpty(format))
                {
                    return Handlers.SerializationFormat.Json;
                }

                return (SerializationFormat)Enum.Parse(typeof(SerializationFormat), format, true);
            }
        }
        
        public override Task<string> GetContentType()
        {
            switch (SerializationFormat)
            {
                case Handlers.SerializationFormat.Jsv:
                    return Task.FromResult<string>("text/plain");
                case Handlers.SerializationFormat.Protobuf:
                    return Task.FromResult<string>("application/x-protobuf");
                default:
                    return Task.FromResult<string>(MimeTypes.JsonMimeType);
            }
        }

        private bool _ObjectToSerializeEnsured = false;
        private T _ObjectToSerialize;
     
        private async Task EnsureObjectToSerialize()
        {
            if (!_ObjectToSerializeEnsured)
            {
                _ObjectToSerialize = await GetObjectToSerialize().ConfigureAwait(false);

                if (_ObjectToSerialize == null)
                {
                    StatusCode = 404;
                }

                _ObjectToSerializeEnsured = true;
            }
        }

        protected abstract Task<T> GetObjectToSerialize();

        protected override Task PrepareResponse()
        {
            return EnsureObjectToSerialize();
        }

        protected async override Task WriteResponseToOutputStream(Stream stream)
        {
            await EnsureObjectToSerialize();

            switch (SerializationFormat)
            {
                case Handlers.SerializationFormat.Jsv:
                    JsvSerializer.SerializeToStream<T>(_ObjectToSerialize, stream);
                    break;
                case Handlers.SerializationFormat.Protobuf:
                    ProtobufSerializer.SerializeToStream<T>(_ObjectToSerialize, stream);
                    break;
                default:
                    JsonSerializer.SerializeToStream<T>(_ObjectToSerialize, stream);
                    break;
            }
        }

        public override bool ShouldCompressResponse(string contentType)
        {
            return SerializationFormat != Handlers.SerializationFormat.Protobuf;
        }
    }

    public enum SerializationFormat
    {
        Json,
        Jsv,
        Protobuf
    }

}

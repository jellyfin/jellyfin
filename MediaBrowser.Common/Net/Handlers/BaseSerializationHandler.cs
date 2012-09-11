using System;
using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Common.Serialization;

namespace MediaBrowser.Common.Net.Handlers
{
    public abstract class BaseSerializationHandler<T> : BaseHandler
    {
        public SerializationFormat SerializationFormat
        {
            get
            {
                string format = QueryString["dataformat"];

                if (string.IsNullOrEmpty(format))
                {
                    return SerializationFormat.Json;
                }

                return (SerializationFormat)Enum.Parse(typeof(SerializationFormat), format, true);
            }
        }
        
        public override Task<string> GetContentType()
        {
            switch (SerializationFormat)
            {
                case SerializationFormat.Jsv:
                    return Task.FromResult("text/plain");
                case SerializationFormat.Protobuf:
                    return Task.FromResult("application/x-protobuf");
                default:
                    return Task.FromResult(MimeTypes.JsonMimeType);
            }
        }

        private bool _ObjectToSerializeEnsured;
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
            await EnsureObjectToSerialize().ConfigureAwait(false);

            switch (SerializationFormat)
            {
                case SerializationFormat.Jsv:
                    JsvSerializer.SerializeToStream(_ObjectToSerialize, stream);
                    break;
                case SerializationFormat.Protobuf:
                    ProtobufSerializer.SerializeToStream(_ObjectToSerialize, stream);
                    break;
                default:
                    JsonSerializer.SerializeToStream(_ObjectToSerialize, stream);
                    break;
            }
        }
    }

    public enum SerializationFormat
    {
        Json,
        Jsv,
        Protobuf
    }

}

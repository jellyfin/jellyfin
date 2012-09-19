using MediaBrowser.Common.Serialization;
using System;
using System.IO;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net.Handlers
{
    public abstract class BaseSerializationHandler<T> : BaseHandler
        where T : class
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

        protected string ContentType
        {
            get
            {
                switch (SerializationFormat)
                {
                    case SerializationFormat.Jsv:
                        return "text/plain";
                    case SerializationFormat.Protobuf:
                        return "application/x-protobuf";
                    default:
                        return MimeTypes.JsonMimeType;
                }
            }
        }

        protected override async Task<ResponseInfo> GetResponseInfo()
        {
            ResponseInfo info = new ResponseInfo
            {
                ContentType = ContentType
            };

            _objectToSerialize = await GetObjectToSerialize().ConfigureAwait(false);

            if (_objectToSerialize == null)
            {
                info.StatusCode = 404;
            }

            return info;
        }

        private T _objectToSerialize;

        protected abstract Task<T> GetObjectToSerialize();

        protected override Task WriteResponseToOutputStream(Stream stream)
        {
            return Task.Run(() =>
            {
                switch (SerializationFormat)
                {
                    case SerializationFormat.Jsv:
                        JsvSerializer.SerializeToStream(_objectToSerialize, stream);
                        break;
                    case SerializationFormat.Protobuf:
                        ProtobufSerializer.SerializeToStream(_objectToSerialize, stream);
                        break;
                    default:
                        JsonSerializer.SerializeToStream(_objectToSerialize, stream);
                        break;
                }
            });
        }
    }

    public enum SerializationFormat
    {
        Json,
        Jsv,
        Protobuf
    }

}

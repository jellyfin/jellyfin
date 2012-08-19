using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Common.Serialization;

namespace MediaBrowser.Common.Net.Handlers
{
    public abstract class BaseJsonHandler<T> : BaseHandler
    {
        public override Task<string> GetContentType()
        {
            return Task.Run(() =>
            {
                return MimeTypes.JsonMimeType;
            });
        }

        private bool _ObjectToSerializeEnsured = false;
        private T _ObjectToSerialize;
     
        private async Task EnsureObjectToSerialize()
        {
            if (!_ObjectToSerializeEnsured)
            {
                _ObjectToSerialize = await GetObjectToSerialize();

                if (_ObjectToSerialize == null)
                {
                    StatusCode = 404;
                }

                _ObjectToSerializeEnsured = true;
            }
        }

        protected abstract Task<T> GetObjectToSerialize();

        protected override async Task PrepareResponse()
        {
            await EnsureObjectToSerialize();
        }

        protected async override Task WriteResponseToOutputStream(Stream stream)
        {
            await EnsureObjectToSerialize();

            JsonSerializer.SerializeToStream<T>(_ObjectToSerialize, stream);
        }
    }
}

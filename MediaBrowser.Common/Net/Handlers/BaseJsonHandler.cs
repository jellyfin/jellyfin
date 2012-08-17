using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Common.Serialization;

namespace MediaBrowser.Common.Net.Handlers
{
    public abstract class BaseJsonHandler<T> : BaseHandler
    {
        public override string ContentType
        {
            get { return MimeTypes.JsonMimeType; }
        }

        private bool _ObjectToSerializeEnsured = false;
        private T _ObjectToSerialize;
     
        private void EnsureObjectToSerialize()
        {
            if (!_ObjectToSerializeEnsured)
            {
                _ObjectToSerialize = GetObjectToSerialize();

                if (_ObjectToSerialize == null)
                {
                    StatusCode = 404;
                }

                _ObjectToSerializeEnsured = true;
            }
        }

        private T ObjectToSerialize
        {
            get
            {
                EnsureObjectToSerialize();
                return _ObjectToSerialize;
            }
        }

        protected abstract T GetObjectToSerialize();

        protected override void PrepareResponse()
        {
            base.PrepareResponse();

            EnsureObjectToSerialize();
        }

        protected override Task WriteResponseToOutputStream(Stream stream)
        {
            return Task.Run(() =>
            {
                JsonSerializer.SerializeToStream<T>(ObjectToSerialize, stream);
            });
        }
    }
}

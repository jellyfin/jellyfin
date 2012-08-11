using System.IO;
using System.Threading.Tasks;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Common.Serialization;

namespace MediaBrowser.Api.HttpHandlers
{
    public abstract class JsonHandler : BaseJsonHandler
    {
        protected abstract object ObjectToSerialize { get; }

        protected override Task WriteResponseToOutputStream(Stream stream)
        {
            return Task.Run(() =>
            {
                JsonSerializer.SerializeToStream(ObjectToSerialize, stream);
            });
        }
    }
}

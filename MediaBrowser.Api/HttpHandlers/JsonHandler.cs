using System.IO;
using MediaBrowser.Common.Json;
using MediaBrowser.Common.Net.Handlers;

namespace MediaBrowser.Api.HttpHandlers
{
    public abstract class JsonHandler : BaseJsonHandler
    {
        protected abstract object ObjectToSerialize { get; }

        protected override void WriteResponseToOutputStream(Stream stream)
        {
            JsonSerializer.SerializeToStream(ObjectToSerialize, stream);
        }
    }
}

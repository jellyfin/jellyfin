using System.IO;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Controller;
using MediaBrowser.Common.Json;

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

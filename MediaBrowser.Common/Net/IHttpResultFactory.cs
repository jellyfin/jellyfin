using System.IO;

namespace MediaBrowser.Common.Net
{
    public interface IHttpResultFactory
    {
        object GetResult(Stream stream, string contentType);
    }
}

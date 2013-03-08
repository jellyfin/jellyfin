using MediaBrowser.Common.Net;
using ServiceStack.Common.Web;
using System.IO;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    public class HttpResultFactory : IHttpResultFactory
    {
        public object GetResult(Stream stream, string contentType)
        {
            return new HttpResult(stream, contentType);
        }
    }
}

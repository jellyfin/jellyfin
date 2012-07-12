using System.Linq;
using System.Net;
using System.IO.Compression;

namespace MediaBrowser.Controller.Net
{
    public class RequestContext
    {
        public HttpListenerRequest Request { get; private set; }
        public HttpListenerResponse Response { get; private set; }

        public RequestContext(HttpListenerContext context)
        {
            Response = context.Response;
            Request = context.Request;
        }

        public void Respond(Response response)
        {
            Response.AddHeader("Access-Control-Allow-Origin", "*");

            foreach (var header in response.Headers)
            {
                Response.AddHeader(header.Key, header.Value);
            }

            Response.ContentType = response.ContentType;
            Response.StatusCode = response.StatusCode;

            Response.SendChunked = true;

            GZipStream gzipStream = new GZipStream(Response.OutputStream, CompressionMode.Compress, false);

            response.WriteStream(Response.OutputStream);
        }
    }
}
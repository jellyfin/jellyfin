using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;

namespace Emby.Server.Implementations.Services
{
    public class HttpResult
        : IHttpResult, IAsyncStreamWriter
    {
        public object Response { get; set; }

        public HttpResult(object response, string contentType, HttpStatusCode statusCode)
        {
            this.Headers = new Dictionary<string, string>();
            this.Cookies = new List<Cookie>();

            this.Response = response;
            this.ContentType = contentType;
            this.StatusCode = statusCode;
        }

        public string ContentType { get; set; }

        public IDictionary<string, string> Headers { get; private set; }

        public List<Cookie> Cookies { get; private set; }

        public int Status { get; set; }

        public HttpStatusCode StatusCode
        {
            get { return (HttpStatusCode)Status; }
            set { Status = (int)value; }
        }

        public IRequest RequestContext { get; set; }

        public async Task WriteToAsync(Stream responseStream, CancellationToken cancellationToken)
        {
            var response = RequestContext != null ? RequestContext.Response : null;

            var bytesResponse = this.Response as byte[];
            if (bytesResponse != null)
            {
                var contentLength = bytesResponse.Length;

                if (response != null)
                    response.SetContentLength(contentLength);

                if (contentLength > 0)
                {
                    await responseStream.WriteAsync(bytesResponse, 0, contentLength).ConfigureAwait(false);
                }
                return;
            }

            await ResponseHelper.WriteObject(this.RequestContext, this.Response, response).ConfigureAwait(false);
        }
    }
}

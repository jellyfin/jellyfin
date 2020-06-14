#pragma warning disable CS1591

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
        public HttpResult(object response, string contentType, HttpStatusCode statusCode)
        {
            this.Headers = new Dictionary<string, string>();

            this.Response = response;
            this.ContentType = contentType;
            this.StatusCode = statusCode;
        }

        public object Response { get; set; }

        public string ContentType { get; set; }

        public IDictionary<string, string> Headers { get; private set; }

        public int Status { get; set; }

        public HttpStatusCode StatusCode
        {
            get => (HttpStatusCode)Status;
            set => Status = (int)value;
        }

        public IRequest RequestContext { get; set; }

        public async Task WriteToAsync(Stream responseStream, CancellationToken cancellationToken)
        {
            var response = RequestContext?.Response;

            if (this.Response is byte[] bytesResponse)
            {
                var contentLength = bytesResponse.Length;

                if (response != null)
                {
                    response.ContentLength = contentLength;
                }

                if (contentLength > 0)
                {
                    await responseStream.WriteAsync(bytesResponse, 0, contentLength, cancellationToken).ConfigureAwait(false);
                }

                return;
            }

            await ResponseHelper.WriteObject(this.RequestContext, this.Response, response).ConfigureAwait(false);
        }
    }
}

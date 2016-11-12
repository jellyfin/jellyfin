using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;
using ServiceStack.Host;

namespace ServiceStack
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
                if (response != null)
                    response.SetContentLength(bytesResponse.Length);

                await responseStream.WriteAsync(bytesResponse, 0, bytesResponse.Length).ConfigureAwait(false);
                return;
            }

            await HttpResponseExtensionsInternal.WriteObject(this.RequestContext, this.Response, response).ConfigureAwait(false);
        }
    }
}

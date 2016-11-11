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
        public HttpResult()
            : this((object)null, null)
        {
        }

        public HttpResult(object response)
            : this(response, null)
        {
        }

        public HttpResult(object response, string contentType)
            : this(response, contentType, HttpStatusCode.OK)
        {
        }

        public HttpResult(HttpStatusCode statusCode, string statusDescription)
            : this()
        {
            StatusCode = statusCode;
            StatusDescription = statusDescription;
        }

        public HttpResult(object response, HttpStatusCode statusCode)
            : this(response, null, statusCode)
        { }

        public HttpResult(object response, string contentType, HttpStatusCode statusCode)
        {
            this.Headers = new Dictionary<string, string>();
            this.Cookies = new List<Cookie>();

            this.Response = response;
            this.ContentType = contentType;
            this.StatusCode = statusCode;
        }

        public HttpResult(Stream responseStream, string contentType)
            : this(null, contentType, HttpStatusCode.OK)
        {
            this.ResponseStream = responseStream;
        }

        public HttpResult(string responseText, string contentType)
            : this(null, contentType, HttpStatusCode.OK)
        {
            this.ResponseText = responseText;
        }

        public HttpResult(byte[] responseBytes, string contentType)
            : this(null, contentType, HttpStatusCode.OK)
        {
            this.ResponseStream = new MemoryStream(responseBytes);
        }

        public string ResponseText { get; private set; }

        public Stream ResponseStream { get; private set; }

        public string ContentType { get; set; }

        public IDictionary<string, string> Headers { get; private set; }

        public List<Cookie> Cookies { get; private set; }

        public string ETag { get; set; }

        public TimeSpan? Age { get; set; }

        public TimeSpan? MaxAge { get; set; }

        public DateTime? Expires { get; set; }

        public DateTime? LastModified { get; set; }

        public Func<IDisposable> ResultScope { get; set; }

        public string Location
        {
            set
            {
                if (StatusCode == HttpStatusCode.OK)
                    StatusCode = HttpStatusCode.Redirect;

                this.Headers["Location"] = value;
            }
        }

        public void SetPermanentCookie(string name, string value)
        {
            SetCookie(name, value, DateTime.UtcNow.AddYears(20), null);
        }

        public void SetPermanentCookie(string name, string value, string path)
        {
            SetCookie(name, value, DateTime.UtcNow.AddYears(20), path);
        }

        public void SetSessionCookie(string name, string value)
        {
            SetSessionCookie(name, value, null);
        }

        public void SetSessionCookie(string name, string value, string path)
        {
            path = path ?? "/";
            this.Headers["Set-Cookie"] = string.Format("{0}={1};path=" + path, name, value);
        }

        public void SetCookie(string name, string value, TimeSpan expiresIn, string path)
        {
            var expiresAt = DateTime.UtcNow.Add(expiresIn);
            SetCookie(name, value, expiresAt, path);
        }

        public void SetCookie(string name, string value, DateTime expiresAt, string path, bool secure = false, bool httpOnly = false)
        {
            path = path ?? "/";
            var cookie = string.Format("{0}={1};expires={2};path={3}", name, value, expiresAt.ToString("R"), path);
            if (secure)
                cookie += ";Secure";
            if (httpOnly)
                cookie += ";HttpOnly";

            this.Headers["Set-Cookie"] = cookie;
        }

        public void DeleteCookie(string name)
        {
            var cookie = string.Format("{0}=;expires={1};path=/", name, DateTime.UtcNow.AddDays(-1).ToString("R"));
            this.Headers["Set-Cookie"] = cookie;
        }

        public int Status { get; set; }

        public HttpStatusCode StatusCode
        {
            get { return (HttpStatusCode)Status; }
            set { Status = (int)value; }
        }

        public string StatusDescription { get; set; }

        public object Response { get; set; }

        public MediaBrowser.Model.Services.IRequest RequestContext { get; set; }

        public string View { get; set; }

        public string Template { get; set; }

        public int PaddingLength { get; set; }

        public async Task WriteToAsync(Stream responseStream, CancellationToken cancellationToken)
        {
            try
            {
                await WriteToInternalAsync(responseStream, cancellationToken).ConfigureAwait(false);
                responseStream.Flush();
            }
            finally
            {
                DisposeStream();
            }
        }

        public static Task WriteTo(Stream inStream, Stream outStream, CancellationToken cancellationToken)
        {
            var memoryStream = inStream as MemoryStream;
            if (memoryStream != null)
            {
                memoryStream.WriteTo(outStream);
                return Task.FromResult(true);
            }

            return inStream.CopyToAsync(outStream, 81920, cancellationToken);
        }

        public async Task WriteToInternalAsync(Stream responseStream, CancellationToken cancellationToken)
        {
            var response = RequestContext != null ? RequestContext.Response : null;

            if (this.ResponseStream != null)
            {
                if (response != null)
                {
                    var ms = ResponseStream as MemoryStream;
                    if (ms != null)
                    {
                        response.SetContentLength(ms.Length);

                        await ms.CopyToAsync(responseStream, 81920, cancellationToken).ConfigureAwait(false);
                        return;
                    }
                }

                await WriteTo(this.ResponseStream, responseStream, cancellationToken).ConfigureAwait(false);
                return;
            }

            if (this.ResponseText != null)
            {
                var bytes = Encoding.UTF8.GetBytes(this.ResponseText);
                if (response != null)
                    response.SetContentLength(bytes.Length);

                await responseStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
                return;
            }

            var bytesResponse = this.Response as byte[];
            if (bytesResponse != null)
            {
                if (response != null)
                    response.SetContentLength(bytesResponse.Length);

                await responseStream.WriteAsync(bytesResponse, 0, bytesResponse.Length).ConfigureAwait(false);
                return;
            }

            ContentTypes.Instance.SerializeToStream(this.RequestContext, this.Response, responseStream);
        }

        private void DisposeStream()
        {
            try
            {
                if (ResponseStream != null)
                {
                    this.ResponseStream.Dispose();
                }
            }
            catch { /*ignore*/ }
        }
    }
}

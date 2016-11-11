using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using MediaBrowser.Model.Services;

namespace ServiceStack.Host
{
    public class HttpResponseStreamWrapper : IHttpResponse
    {
        private static readonly UTF8Encoding UTF8EncodingWithoutBom = new UTF8Encoding(false);

        public HttpResponseStreamWrapper(Stream stream, IRequest request)
        {
            this.OutputStream = stream;
            this.Request = request;
            this.Headers = new Dictionary<string, string>();
            this.Items = new Dictionary<string, object>();
        }

        public Dictionary<string, string> Headers { get; set; }

        public object OriginalResponse
        {
            get { return null; }
        }

        public IRequest Request { get; private set; }

        public int StatusCode { set; get; }
        public string StatusDescription { set; get; }
        public string ContentType { get; set; }

        public void AddHeader(string name, string value)
        {
            this.Headers[name] = value;
        }

        public string GetHeader(string name)
        {
            return this.Headers[name];
        }

        public void Redirect(string url)
        {
            this.Headers["Location"] = url;
        }

        public Stream OutputStream { get; private set; }

        public object Dto { get; set; }

        public void Write(string text)
        {
            var bytes = UTF8EncodingWithoutBom.GetBytes(text);
            OutputStream.Write(bytes, 0, bytes.Length);
        }

        public bool UseBufferedStream { get; set; }

        public void Close()
        {
            if (IsClosed) return;

            OutputStream.Dispose();
            IsClosed = true;
        }

        public void End()
        {
            Close();
        }

        public void Flush()
        {
            OutputStream.Flush();
        }

        public bool IsClosed { get; private set; }

        public void SetContentLength(long contentLength) {}

        public bool KeepAlive { get; set; }

        public Dictionary<string, object> Items { get; private set; }

        public void SetCookie(Cookie cookie)
        {
        }

        public void ClearCookies()
        {
        }
    }
}
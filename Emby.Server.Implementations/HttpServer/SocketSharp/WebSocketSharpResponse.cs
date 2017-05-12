using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Services;
using SocketHttpListener.Net;
using HttpListenerResponse = SocketHttpListener.Net.HttpListenerResponse;
using IHttpResponse = MediaBrowser.Model.Services.IHttpResponse;
using IRequest = MediaBrowser.Model.Services.IRequest;

namespace Emby.Server.Implementations.HttpServer.SocketSharp
{
    public class WebSocketSharpResponse : IHttpResponse
    {
        private readonly ILogger _logger;
        private readonly HttpListenerResponse _response;

        public WebSocketSharpResponse(ILogger logger, HttpListenerResponse response, IRequest request)
        {
            _logger = logger;
            this._response = response;
            Items = new Dictionary<string, object>();
            Request = request;
        }

        public IRequest Request { get; private set; }
        public bool UseBufferedStream { get; set; }
        public Dictionary<string, object> Items { get; private set; }
        public object OriginalResponse
        {
            get { return _response; }
        }

        public int StatusCode
        {
            get { return this._response.StatusCode; }
            set { this._response.StatusCode = value; }
        }

        public string StatusDescription
        {
            get { return this._response.StatusDescription; }
            set { this._response.StatusDescription = value; }
        }

        public string ContentType
        {
            get { return _response.ContentType; }
            set { _response.ContentType = value; }
        }

        //public ICookies Cookies { get; set; }

        public void AddHeader(string name, string value)
        {
            if (string.Equals(name, "Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                ContentType = value;
                return;
            }

            _response.AddHeader(name, value);
        }

        public QueryParamCollection Headers
        {
            get
            {
                return _response.Headers;
            }
        }

        public string GetHeader(string name)
        {
            return _response.Headers[name];
        }

        public void Redirect(string url)
        {
            _response.Redirect(url);
        }

        public Stream OutputStream
        {
            get { return _response.OutputStream; }
        }

        public void Close()
        {
            if (!this.IsClosed)
            {
                this.IsClosed = true;

                try
                {
                    CloseOutputStream(this._response);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error closing HttpListener output stream", ex);
                }
            }
        }

        public void CloseOutputStream(HttpListenerResponse response)
        {
            try
            {
                var outputStream = response.OutputStream;

                // This is needed with compression
                if (outputStream is ResponseStream)
                {
                    //if (!string.IsNullOrWhiteSpace(GetHeader("Content-Encoding")))
                    {
                        outputStream.Flush();
                    }

                    outputStream.Dispose();
                }
                response.Close();
            }
            catch (Exception ex)
            {
                _logger.ErrorException("Error in HttpListenerResponseWrapper: " + ex.Message, ex);
            }
        }

        public bool IsClosed
        {
            get;
            private set;
        }

        public void SetContentLength(long contentLength)
        {
            //you can happily set the Content-Length header in Asp.Net
            //but HttpListener will complain if you do - you have to set ContentLength64 on the response.
            //workaround: HttpListener throws "The parameter is incorrect" exceptions when we try to set the Content-Length header
            _response.ContentLength64 = contentLength;
        }

        public void SetCookie(Cookie cookie)
        {
            var cookieStr = AsHeaderValue(cookie);
            _response.Headers.Add("Set-Cookie", cookieStr);
        }

        public static string AsHeaderValue(Cookie cookie)
        {
            var defaultExpires = DateTime.MinValue;

            var path = cookie.Expires == defaultExpires
                ? "/"
                : cookie.Path ?? "/";

            var sb = new StringBuilder();

            sb.Append($"{cookie.Name}={cookie.Value};path={path}");

            if (cookie.Expires != defaultExpires)
            {
                sb.Append($";expires={cookie.Expires:R}");
            }

            if (!string.IsNullOrEmpty(cookie.Domain))
            {
                sb.Append($";domain={cookie.Domain}");
            }
            //else if (restrictAllCookiesToDomain != null)
            //{
            //    sb.Append($";domain={restrictAllCookiesToDomain}");
            //}

            if (cookie.Secure)
            {
                sb.Append(";Secure");
            }
            if (cookie.HttpOnly)
            {
                sb.Append(";HttpOnly");
            }

            return sb.ToString();
        }


        public bool SendChunked
        {
            get { return _response.SendChunked; }
            set { _response.SendChunked = value; }
        }

        public bool KeepAlive { get; set; }

        public void ClearCookies()
        {
        }

        public Task TransmitFile(string path, long offset, long count, FileShareMode fileShareMode, CancellationToken cancellationToken)
        {
            return _response.TransmitFile(path, offset, count, fileShareMode, cancellationToken);
        }
    }
}

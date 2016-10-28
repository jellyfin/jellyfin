using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using MediaBrowser.Model.Logging;
using ServiceStack;
using ServiceStack.Host;
using HttpListenerResponse = SocketHttpListener.Net.HttpListenerResponse;
using IHttpResponse = MediaBrowser.Model.Services.IHttpResponse;
using IRequest = MediaBrowser.Model.Services.IRequest;

namespace MediaBrowser.Server.Implementations.HttpServer.SocketSharp
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

        public object Dto { get; set; }

        public void Write(string text)
        {
            var bOutput = System.Text.Encoding.UTF8.GetBytes(text);
            _response.ContentLength64 = bOutput.Length;

            var outputStream = _response.OutputStream;
            outputStream.Write(bOutput, 0, bOutput.Length);
            Close();
        }

        public void Close()
        {
            if (!this.IsClosed)
            {
                this.IsClosed = true;

                try
                {
                    this._response.CloseOutputStream(_logger);
                }
                catch (Exception ex)
                {
                    _logger.ErrorException("Error closing HttpListener output stream", ex);
                }
            }
        }

        public void End()
        {
            Close();
        }

        public void Flush()
        {
            _response.OutputStream.Flush();
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
            var cookieStr = cookie.AsHeaderValue();
            _response.Headers.Add(HttpHeaders.SetCookie, cookieStr);
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
    }
}

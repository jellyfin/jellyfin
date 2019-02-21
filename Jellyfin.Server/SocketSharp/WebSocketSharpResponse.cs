using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;
using Microsoft.Extensions.Logging;
using IHttpResponse = MediaBrowser.Model.Services.IHttpResponse;
using IRequest = MediaBrowser.Model.Services.IRequest;

namespace Jellyfin.Server.SocketSharp
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

        public Dictionary<string, object> Items { get; private set; }

        public object OriginalResponse => _response;

        public int StatusCode
        {
            get => this._response.StatusCode;
            set => this._response.StatusCode = value;
        }

        public string StatusDescription
        {
            get => this._response.StatusDescription;
            set => this._response.StatusDescription = value;
        }

        public string ContentType
        {
            get => _response.ContentType;
            set => _response.ContentType = value;
        }

        public QueryParamCollection Headers => new QueryParamCollection(_response.Headers);

        private static string AsHeaderValue(Cookie cookie)
        {
            DateTime defaultExpires = DateTime.MinValue;

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

        public Stream OutputStream => _response.OutputStream;

        public void Close()
        {
            if (!this.IsClosed)
            {
                this.IsClosed = true;

                try
                {
                    var response = this._response;

                    var outputStream = response.OutputStream;

                    // This is needed with compression
                    outputStream.Flush();
                    outputStream.Dispose();

                    response.Close();
                }
                catch (SocketException)
                {
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in HttpListenerResponseWrapper");
                }
            }
        }

        public bool IsClosed
        {
            get;
            private set;
        }

        public void SetContentLength(long contentLength)
        {
            // you can happily set the Content-Length header in Asp.Net
            // but HttpListener will complain if you do - you have to set ContentLength64 on the response.
            // workaround: HttpListener throws "The parameter is incorrect" exceptions when we try to set the Content-Length header
            //_response.ContentLength64 = contentLength;
        }

        public void SetCookie(Cookie cookie)
        {
            var cookieStr = AsHeaderValue(cookie);
            _response.Headers.Add("Set-Cookie", cookieStr);
        }

        public bool SendChunked
        {
            get => _response.SendChunked;
            set => _response.SendChunked = value;
        }

        public bool KeepAlive { get; set; }

        public void ClearCookies()
        {
        }
        const int StreamCopyToBufferSize = 81920;
        public async Task TransmitFile(string path, long offset, long count, FileShareMode fileShareMode, IFileSystem fileSystem, IStreamHelper streamHelper, CancellationToken cancellationToken)
        {
            // TODO
            // return _response.TransmitFile(path, offset, count, fileShareMode, cancellationToken);
            var allowAsync = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

            //if (count <= 0)
            //{
            //    allowAsync = true;
            //}

            var fileOpenOptions = FileOpenOptions.SequentialScan;

            if (allowAsync)
            {
                fileOpenOptions |= FileOpenOptions.Asynchronous;
            }

            // use non-async filestream along with read due to https://github.com/dotnet/corefx/issues/6039

            using (var fs = fileSystem.GetFileStream(path, FileOpenMode.Open, FileAccessMode.Read, fileShareMode, fileOpenOptions))
            {
                if (offset > 0)
                {
                    fs.Position = offset;
                }
                
                if (count > 0)
                {
                    await streamHelper.CopyToAsync(fs, OutputStream, count, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await fs.CopyToAsync(OutputStream, StreamCopyToBufferSize, cancellationToken).ConfigureAwait(false);
                }
            }
        }
    }
}

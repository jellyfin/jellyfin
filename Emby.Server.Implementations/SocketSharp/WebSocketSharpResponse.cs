using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using IRequest = MediaBrowser.Model.Services.IRequest;

namespace Emby.Server.Implementations.SocketSharp
{
    public class WebSocketSharpResponse : IResponse
    {
        private readonly ILogger _logger;

        private readonly HttpResponse _response;

        public WebSocketSharpResponse(ILogger logger, HttpResponse response, IRequest request)
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

        public string StatusDescription { get; set; }

        public string ContentType
        {
            get => _response.ContentType;
            set => _response.ContentType = value;
        }

        public IHeaderDictionary Headers => _response.Headers;

        public void AddHeader(string name, string value)
        {
            if (string.Equals(name, "Content-Type", StringComparison.OrdinalIgnoreCase))
            {
                ContentType = value;
                return;
            }

            _response.Headers.Add(name, value);
        }

        public string GetHeader(string name)
        {
            return _response.Headers[name];
        }

        public void Redirect(string url)
        {
            _response.Redirect(url);
        }

        public Stream OutputStream => _response.Body;

        public bool IsClosed { get; set; }

        public bool SendChunked { get; set; }

        const int StreamCopyToBufferSize = 81920;
        public async Task TransmitFile(string path, long offset, long count, FileShareMode fileShareMode, IFileSystem fileSystem, IStreamHelper streamHelper, CancellationToken cancellationToken)
        {
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

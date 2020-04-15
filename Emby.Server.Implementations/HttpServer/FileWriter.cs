using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Emby.Server.Implementations.HttpServer
{
    /// <summary>
    /// A file writer.
    /// </summary>
    public class FileWriter : IHttpResult
    {
        private static readonly CultureInfo UsCulture = CultureInfo.ReadOnly(new CultureInfo("en-US"));

        private static readonly string[] _skipLogExtensions = {
            ".js",
            ".html",
            ".css"
        };

        private readonly IStreamHelper _streamHelper;
        private readonly ILogger _logger;

        /// <summary>
        /// The _options
        /// </summary>
        private readonly IDictionary<string, string> _options = new Dictionary<string, string>();

        /// <summary>
        /// The _requested ranges
        /// </summary>
        private List<KeyValuePair<long, long?>> _requestedRanges;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileWriter"/> class.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="contentType">The content type.</param>
        /// <param name="rangeHeader">The range header.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The filesystem.</param>
        /// <param name="streamHelper">The stream helper.</param>
        /// <exception cref="ArgumentNullException">If contentType is null or empty.</exception>
        public FileWriter(string path, string contentType, string rangeHeader, ILogger logger, IFileSystem fileSystem, IStreamHelper streamHelper)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            _streamHelper = streamHelper;

            Path = path;
            _logger = logger;
            RangeHeader = rangeHeader;

            Headers[HeaderNames.ContentType] = contentType;

            TotalContentLength = fileSystem.GetFileInfo(path).Length;
            Headers[HeaderNames.AcceptRanges] = "bytes";

            if (string.IsNullOrWhiteSpace(rangeHeader))
            {
                Headers[HeaderNames.ContentLength] = TotalContentLength.ToString(CultureInfo.InvariantCulture);
                StatusCode = HttpStatusCode.OK;
            }
            else
            {
                StatusCode = HttpStatusCode.PartialContent;
                SetRangeValues();
            }

            FileShare = FileShare.Read;
            Cookies = new List<Cookie>();
        }

        private string RangeHeader { get; set; }

        private bool IsHeadRequest { get; set; }

        private long RangeStart { get; set; }

        private long RangeEnd { get; set; }

        private long RangeLength { get; set; }

        /// <summary>
        /// The total content length.
        /// </summary>
        public long TotalContentLength { get; set; }

        /// <summary>
        /// The OnComplete action.
        /// </summary>
        public Action OnComplete { get; set; }

        /// <summary>
        /// The OnError action.
        /// </summary>
        public Action OnError { get; set; }

        /// <summary>
        /// The cookies.
        /// </summary>
        public List<Cookie> Cookies { get; private set; }

        /// <summary>
        /// The file share.
        /// </summary>
        public FileShare FileShare { get; set; }

        /// <summary>
        /// Gets the options.
        /// </summary>
        /// <value>The options.</value>
        public IDictionary<string, string> Headers => _options;

        /// <summary>
        /// The path.
        /// </summary>
        public string Path { get; set; }

        /// <summary>
        /// Gets the requested ranges.
        /// </summary>
        /// <value>The requested ranges.</value>
        protected List<KeyValuePair<long, long?>> RequestedRanges
        {
            get
            {
                if (_requestedRanges == null)
                {
                    _requestedRanges = new List<KeyValuePair<long, long?>>();

                    // Example: bytes=0-,32-63
                    var ranges = RangeHeader.Split('=')[1].Split(',');

                    foreach (var range in ranges)
                    {
                        var vals = range.Split('-');

                        long start = 0;
                        long? end = null;

                        if (!string.IsNullOrEmpty(vals[0]))
                        {
                            start = long.Parse(vals[0], UsCulture);
                        }

                        if (!string.IsNullOrEmpty(vals[1]))
                        {
                            end = long.Parse(vals[1], UsCulture);
                        }

                        _requestedRanges.Add(new KeyValuePair<long, long?>(start, end));
                    }
                }

                return _requestedRanges;
            }
        }

        /// <inheritdoc />
        public string ContentType { get; set; }

        /// <inheritdoc />
        public IRequest RequestContext { get; set; }

        /// <inheritdoc />
        public object Response { get; set; }

        /// <inheritdoc />
        public int Status { get; set; }

        /// <inheritdoc />
        public HttpStatusCode StatusCode
        {
            get => (HttpStatusCode)Status;
            set => Status = (int)value;
        }

        /// <summary>
        /// Sets the range values.
        /// </summary>
        private void SetRangeValues()
        {
            var requestedRange = RequestedRanges[0];

            // If the requested range is "0-", we can optimize by just doing a stream copy
            if (!requestedRange.Value.HasValue)
            {
                RangeEnd = TotalContentLength - 1;
            }
            else
            {
                RangeEnd = requestedRange.Value.Value;
            }

            RangeStart = requestedRange.Key;
            RangeLength = 1 + RangeEnd - RangeStart;

            // Content-Length is the length of what we're serving, not the original content
            var lengthString = RangeLength.ToString(CultureInfo.InvariantCulture);
            Headers[HeaderNames.ContentLength] = lengthString;
            var rangeString = $"bytes {RangeStart}-{RangeEnd}/{TotalContentLength}";
            Headers[HeaderNames.ContentRange] = rangeString;

            _logger.LogDebug("Setting range response values for {0}. RangeRequest: {1} Content-Length: {2}, Content-Range: {3}", Path, RangeHeader, lengthString, rangeString);
        }

        /// <summary>
        /// Writes the given response.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The completed task.</returns>
        public async Task WriteToAsync(HttpResponse response, CancellationToken cancellationToken)
        {
            try
            {
                // Headers only
                if (IsHeadRequest)
                {
                    return;
                }

                var path = Path;
                var offset = RangeStart;
                var count = RangeLength;

                if (string.IsNullOrWhiteSpace(RangeHeader) || (RangeStart <= 0 && RangeEnd >= TotalContentLength - 1))
                {
                    var extension = System.IO.Path.GetExtension(path);

                    if (extension == null || !_skipLogExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                    {
                        _logger.LogDebug("Transmit file {0}", path);
                    }

                    offset = 0;
                    count = 0;
                }

                await TransmitFile(response.Body, path, offset, count, FileShare, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                OnComplete?.Invoke();
            }
        }

        /// <summary>
        /// Transmits the provided file.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="path">The path.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="count">The count.</param>
        /// <param name="fileShare">The fileshare.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The completed task.</returns>
        public async Task TransmitFile(Stream stream, string path, long offset, long count, FileShare fileShare, CancellationToken cancellationToken)
        {
            var fileOptions = FileOptions.SequentialScan;

            // use non-async filestream along with read due to https://github.com/dotnet/corefx/issues/6039
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                fileOptions |= FileOptions.Asynchronous;
            }

            await using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, fileShare, IODefaults.FileStreamBufferSize, fileOptions);
            if (offset > 0)
            {
                fs.Position = offset;
            }

            if (count > 0)
            {
                await _streamHelper.CopyToAsync(fs, stream, count, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await fs.CopyToAsync(stream, IODefaults.CopyToBufferSize, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}

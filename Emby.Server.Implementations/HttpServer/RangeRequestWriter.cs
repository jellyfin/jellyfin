#pragma warning disable CS1591

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Services;
using Microsoft.Net.Http.Headers;

namespace Emby.Server.Implementations.HttpServer
{
    public class RangeRequestWriter : IAsyncStreamWriter, IHttpResult
    {
        private const int BufferSize = 81920;

        private readonly Dictionary<string, string> _options = new Dictionary<string, string>();

        private List<KeyValuePair<long, long?>> _requestedRanges;

        /// <summary>
        /// Initializes a new instance of the <see cref="RangeRequestWriter" /> class.
        /// </summary>
        /// <param name="rangeHeader">The range header.</param>
        /// <param name="contentLength">The content length.</param>
        /// <param name="source">The source.</param>
        /// <param name="contentType">Type of the content.</param>
        /// <param name="isHeadRequest">if set to <c>true</c> [is head request].</param>
        public RangeRequestWriter(string rangeHeader, long contentLength, Stream source, string contentType, bool isHeadRequest)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                throw new ArgumentNullException(nameof(contentType));
            }

            RangeHeader = rangeHeader;
            SourceStream = source;
            IsHeadRequest = isHeadRequest;

            ContentType = contentType;
            Headers[HeaderNames.ContentType] = contentType;
            Headers[HeaderNames.AcceptRanges] = "bytes";
            StatusCode = HttpStatusCode.PartialContent;

            SetRangeValues(contentLength);
        }

        /// <summary>
        /// Gets or sets the source stream.
        /// </summary>
        /// <value>The source stream.</value>
        private Stream SourceStream { get; set; }
        private string RangeHeader { get; set; }
        private bool IsHeadRequest { get; set; }

        private long RangeStart { get; set; }
        private long RangeEnd { get; set; }
        private long RangeLength { get; set; }
        private long TotalContentLength { get; set; }

        public Action OnComplete { get; set; }

        /// <summary>
        /// Additional HTTP Headers
        /// </summary>
        /// <value>The headers.</value>
        public IDictionary<string, string> Headers => _options;

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
                            start = long.Parse(vals[0], CultureInfo.InvariantCulture);
                        }

                        if (!string.IsNullOrEmpty(vals[1]))
                        {
                            end = long.Parse(vals[1], CultureInfo.InvariantCulture);
                        }

                        _requestedRanges.Add(new KeyValuePair<long, long?>(start, end));
                    }
                }

                return _requestedRanges;
            }
        }

        public string ContentType { get; set; }

        public IRequest RequestContext { get; set; }

        public object Response { get; set; }

        public int Status { get; set; }

        public HttpStatusCode StatusCode
        {
            get => (HttpStatusCode)Status;
            set => Status = (int)value;
        }

        /// <summary>
        /// Sets the range values.
        /// </summary>
        private void SetRangeValues(long contentLength)
        {
            var requestedRange = RequestedRanges[0];

            TotalContentLength = contentLength;

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

            Headers[HeaderNames.ContentLength] = RangeLength.ToString(CultureInfo.InvariantCulture);
            Headers[HeaderNames.ContentRange] = $"bytes {RangeStart}-{RangeEnd}/{TotalContentLength}";

            if (RangeStart > 0 && SourceStream.CanSeek)
            {
                SourceStream.Position = RangeStart;
            }
        }

        public async Task WriteToAsync(Stream responseStream, CancellationToken cancellationToken)
        {
            try
            {
                // Headers only
                if (IsHeadRequest)
                {
                    return;
                }

                using (var source = SourceStream)
                {
                    // If the requested range is "0-", we can optimize by just doing a stream copy
                    if (RangeEnd >= TotalContentLength - 1)
                    {
                        await source.CopyToAsync(responseStream, BufferSize, cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await CopyToInternalAsync(source, responseStream, RangeLength, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally
            {
                OnComplete?.Invoke();
            }
        }

        private static async Task CopyToInternalAsync(Stream source, Stream destination, long copyLength, CancellationToken cancellationToken)
        {
            var array = ArrayPool<byte>.Shared.Rent(BufferSize);
            try
            {
                int bytesRead;
                while ((bytesRead = await source.ReadAsync(array, 0, array.Length, cancellationToken).ConfigureAwait(false)) != 0)
                {
                    var bytesToCopy = Math.Min(bytesRead, copyLength);

                    await destination.WriteAsync(array, 0, Convert.ToInt32(bytesToCopy), cancellationToken).ConfigureAwait(false);

                    copyLength -= bytesToCopy;

                    if (copyLength <= 0)
                    {
                        break;
                    }
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(array);
            }
        }
    }
}

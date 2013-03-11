using ServiceStack.Service;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    public class RangeRequestWriter : IStreamWriter
    {
        /// <summary>
        /// Gets or sets the source stream.
        /// </summary>
        /// <value>The source stream.</value>
        public Stream SourceStream { get; set; }
        public HttpListenerResponse Response { get; set; }
        public NameValueCollection RequestHeaders { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StreamWriter" /> class.
        /// </summary>
        /// <param name="requestHeaders">The request headers.</param>
        /// <param name="response">The response.</param>
        /// <param name="source">The source.</param>
        public RangeRequestWriter(NameValueCollection requestHeaders, HttpListenerResponse response, Stream source)
        {
            RequestHeaders = requestHeaders;
            Response = response;
            SourceStream = source;
        }

        /// <summary>
        /// The _requested ranges
        /// </summary>
        private List<KeyValuePair<long, long?>> _requestedRanges;
        /// <summary>
        /// Gets the requested ranges.
        /// </summary>
        /// <value>The requested ranges.</value>
        protected IEnumerable<KeyValuePair<long, long?>> RequestedRanges
        {
            get
            {
                if (_requestedRanges == null)
                {
                    _requestedRanges = new List<KeyValuePair<long, long?>>();

                    // Example: bytes=0-,32-63
                    var ranges = RequestHeaders["Range"].Split('=')[1].Split(',');

                    foreach (var range in ranges)
                    {
                        var vals = range.Split('-');

                        long start = 0;
                        long? end = null;

                        if (!string.IsNullOrEmpty(vals[0]))
                        {
                            start = long.Parse(vals[0]);
                        }
                        if (!string.IsNullOrEmpty(vals[1]))
                        {
                            end = long.Parse(vals[1]);
                        }

                        _requestedRanges.Add(new KeyValuePair<long, long?>(start, end));
                    }
                }

                return _requestedRanges;
            }
        }

        /// <summary>
        /// Writes to.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        public void WriteTo(Stream responseStream)
        {
            Response.Headers["Accept-Ranges"] = "bytes";
            Response.StatusCode = 206;
            
            var task = WriteToAsync(responseStream);

            Task.WaitAll(task);
        }

        /// <summary>
        /// Writes to async.
        /// </summary>
        /// <param name="responseStream">The response stream.</param>
        /// <returns>Task.</returns>
        private Task WriteToAsync(Stream responseStream)
        {
            var requestedRange = RequestedRanges.First();

            var totalLength = SourceStream.Length;

            // If the requested range is "0-", we can optimize by just doing a stream copy
            if (!requestedRange.Value.HasValue)
            {
                return ServeCompleteRangeRequest(requestedRange, responseStream, totalLength);
            }

            // This will have to buffer a portion of the content into memory
            return ServePartialRangeRequest(requestedRange.Key, requestedRange.Value.Value, responseStream, totalLength);
        }

        /// <summary>
        /// Handles a range request of "bytes=0-"
        /// This will serve the complete content and add the content-range header
        /// </summary>
        /// <param name="requestedRange">The requested range.</param>
        /// <param name="responseStream">The response stream.</param>
        /// <param name="totalContentLength">Total length of the content.</param>
        /// <returns>Task.</returns>
        private Task ServeCompleteRangeRequest(KeyValuePair<long, long?> requestedRange, Stream responseStream, long totalContentLength)
        {
            var rangeStart = requestedRange.Key;
            var rangeEnd = totalContentLength - 1;
            var rangeLength = 1 + rangeEnd - rangeStart;

            // Content-Length is the length of what we're serving, not the original content
            Response.ContentLength64 = rangeLength;
            Response.Headers["Content-Range"] = string.Format("bytes {0}-{1}/{2}", rangeStart, rangeEnd, totalContentLength);

            if (rangeStart > 0)
            {
                SourceStream.Position = rangeStart;
            }

            return SourceStream.CopyToAsync(responseStream);
        }

        /// <summary>
        /// Serves a partial range request
        /// </summary>
        /// <param name="rangeStart">The range start.</param>
        /// <param name="rangeEnd">The range end.</param>
        /// <param name="responseStream">The response stream.</param>
        /// <param name="totalContentLength">Total length of the content.</param>
        /// <returns>Task.</returns>
        private async Task ServePartialRangeRequest(long rangeStart, long rangeEnd, Stream responseStream, long totalContentLength)
        {
            var rangeLength = 1 + rangeEnd - rangeStart;

            // Content-Length is the length of what we're serving, not the original content
            Response.ContentLength64 = rangeLength;
            Response.Headers["Content-Range"] = string.Format("bytes {0}-{1}/{2}", rangeStart, rangeEnd, totalContentLength);

            SourceStream.Position = rangeStart;

            // Fast track to just copy the stream to the end
            if (rangeEnd == totalContentLength - 1)
            {
                await SourceStream.CopyToAsync(responseStream).ConfigureAwait(false);
            }
            else
            {
                // Read the bytes we need
                var buffer = new byte[Convert.ToInt32(rangeLength)];
                await SourceStream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                await responseStream.WriteAsync(buffer, 0, Convert.ToInt32(rangeLength)).ConfigureAwait(false);
            }
        }
    }
}

using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.IO;
using MediaBrowser.Common.Kernel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net.Handlers
{
    /// <summary>
    /// Represents an http handler that serves static content
    /// </summary>
    public class StaticFileHandler : BaseHandler<IKernel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFileHandler" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        public StaticFileHandler(IKernel kernel)
        {
            Initialize(kernel);
        }

        /// <summary>
        /// The _path
        /// </summary>
        private string _path;
        /// <summary>
        /// Gets or sets the path to the static resource
        /// </summary>
        /// <value>The path.</value>
        public string Path
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_path))
                {
                    return _path;
                }

                return QueryString["path"];
            }
            set
            {
                _path = value;
            }
        }

        /// <summary>
        /// Gets or sets the last date modified of the resource
        /// </summary>
        /// <value>The last date modified.</value>
        public DateTime? LastDateModified { get; set; }

        /// <summary>
        /// Gets or sets the content type of the resource
        /// </summary>
        /// <value>The type of the content.</value>
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the content type of the resource
        /// </summary>
        /// <value>The etag.</value>
        public Guid Etag { get; set; }

        /// <summary>
        /// Gets or sets the source stream of the resource
        /// </summary>
        /// <value>The source stream.</value>
        public Stream SourceStream { get; set; }

        /// <summary>
        /// Shoulds the compress response.
        /// </summary>
        /// <param name="contentType">Type of the content.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private bool ShouldCompressResponse(string contentType)
        {
            // It will take some work to support compression with byte range requests
            if (IsRangeRequest)
            {
                return false;
            }

            // Don't compress media
            if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) || contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // Don't compress images
            if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets or sets the duration of the cache.
        /// </summary>
        /// <value>The duration of the cache.</value>
        public TimeSpan? CacheDuration { get; set; }

        /// <summary>
        /// Gets the total length of the content.
        /// </summary>
        /// <param name="responseInfo">The response info.</param>
        /// <returns>System.Nullable{System.Int64}.</returns>
        protected override long? GetTotalContentLength(ResponseInfo responseInfo)
        {
            // If we're compressing the response, content length must be the compressed length, which we don't know
            if (responseInfo.CompressResponse && ClientSupportsCompression)
            {
                return null;
            }

            return SourceStream.Length;
        }

        /// <summary>
        /// Gets the response info.
        /// </summary>
        /// <returns>Task{ResponseInfo}.</returns>
        protected override Task<ResponseInfo> GetResponseInfo()
        {
            var info = new ResponseInfo
            {
                ContentType = ContentType ?? MimeTypes.GetMimeType(Path),
                Etag = Etag,
                DateLastModified = LastDateModified
            };

            if (SourceStream == null && !string.IsNullOrEmpty(Path))
            {
                // FileShare must be ReadWrite in case someone else is currently writing to it.
                SourceStream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous);
            }

            info.CompressResponse = ShouldCompressResponse(info.ContentType);

            info.SupportsByteRangeRequests = !info.CompressResponse || !ClientSupportsCompression;

            if (!info.DateLastModified.HasValue && !string.IsNullOrWhiteSpace(Path))
            {
                info.DateLastModified = File.GetLastWriteTimeUtc(Path);
            }

            if (CacheDuration.HasValue)
            {
                info.CacheDuration = CacheDuration.Value;
            }

            if (SourceStream == null && string.IsNullOrEmpty(Path))
            {
                throw new ResourceNotFoundException();
            }

            return Task.FromResult(info);
        }

        /// <summary>
        /// Writes the response to output stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="responseInfo">The response info.</param>
        /// <param name="totalContentLength">Total length of the content.</param>
        /// <returns>Task.</returns>
        protected override Task WriteResponseToOutputStream(Stream stream, ResponseInfo responseInfo, long? totalContentLength)
        {
            if (IsRangeRequest && totalContentLength.HasValue)
            {
                var requestedRange = RequestedRanges.First();

                // If the requested range is "0-", we can optimize by just doing a stream copy
                if (!requestedRange.Value.HasValue)
                {
                    return ServeCompleteRangeRequest(requestedRange, stream, totalContentLength.Value);
                }

                // This will have to buffer a portion of the content into memory
                return ServePartialRangeRequest(requestedRange.Key, requestedRange.Value.Value, stream, totalContentLength.Value);
            }

            return SourceStream.CopyToAsync(stream);
        }

        /// <summary>
        /// Disposes the response stream.
        /// </summary>
        protected override void DisposeResponseStream()
        {
            if (SourceStream != null)
            {
                SourceStream.Dispose();
            }

            base.DisposeResponseStream();
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
            HttpListenerContext.Response.ContentLength64 = rangeLength;
            HttpListenerContext.Response.Headers["Content-Range"] = string.Format("bytes {0}-{1}/{2}", rangeStart, rangeEnd, totalContentLength);

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
            HttpListenerContext.Response.ContentLength64 = rangeLength;
            HttpListenerContext.Response.Headers["Content-Range"] = string.Format("bytes {0}-{1}/{2}", rangeStart, rangeEnd, totalContentLength);

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

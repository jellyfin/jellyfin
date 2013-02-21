using MediaBrowser.Common.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net.Handlers
{
    public class StaticFileHandler : BaseHandler
    {
        public override bool HandlesRequest(HttpListenerRequest request)
        {
            return false;
        }

        private string _path;
        public virtual string Path
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

        private Stream SourceStream { get; set; }

        protected override bool SupportsByteRangeRequests
        {
            get
            {
                return true;
            }
        }

        private bool ShouldCompressResponse(string contentType)
        {
            // Can't compress these
            if (IsRangeRequest)
            {
                return false;
            }

            // Don't compress media
            if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase) || contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            // It will take some work to support compression within this handler
            return false;
        }

        protected override long? GetTotalContentLength()
        {
            return SourceStream.Length;
        }

        protected override Task<ResponseInfo> GetResponseInfo()
        {
            ResponseInfo info = new ResponseInfo
            {
                ContentType = MimeTypes.GetMimeType(Path),
            };

            try
            {
                SourceStream = File.OpenRead(Path);
            }
            catch (FileNotFoundException ex)
            {
                info.StatusCode = 404;
                Logger.LogException(ex);
            }
            catch (DirectoryNotFoundException ex)
            {
                info.StatusCode = 404;
                Logger.LogException(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                info.StatusCode = 403;
                Logger.LogException(ex);
            }

            info.CompressResponse = ShouldCompressResponse(info.ContentType);

            if (SourceStream != null)
            {
                info.DateLastModified = File.GetLastWriteTimeUtc(Path);
            }

            return Task.FromResult<ResponseInfo>(info);
        }

        protected override Task WriteResponseToOutputStream(Stream stream)
        {
            if (IsRangeRequest)
            {
                KeyValuePair<long, long?> requestedRange = RequestedRanges.First();

                // If the requested range is "0-" and we know the total length, we can optimize by avoiding having to buffer the content into memory
                if (requestedRange.Value == null && TotalContentLength != null)
                {
                    return ServeCompleteRangeRequest(requestedRange, stream);
                }
                if (TotalContentLength.HasValue)
                {
                    // This will have to buffer a portion of the content into memory
                    return ServePartialRangeRequestWithKnownTotalContentLength(requestedRange, stream);
                }

                // This will have to buffer the entire content into memory
                return ServePartialRangeRequestWithUnknownTotalContentLength(requestedRange, stream);
            }

            return SourceStream.CopyToAsync(stream);
        }

        protected override void DisposeResponseStream()
        {
            base.DisposeResponseStream();

            if (SourceStream != null)
            {
                SourceStream.Dispose();
            }
        }

        /// <summary>
        /// Handles a range request of "bytes=0-"
        /// This will serve the complete content and add the content-range header
        /// </summary>
        private Task ServeCompleteRangeRequest(KeyValuePair<long, long?> requestedRange, Stream responseStream)
        {
            long totalContentLength = TotalContentLength.Value;

            long rangeStart = requestedRange.Key;
            long rangeEnd = totalContentLength - 1;
            long rangeLength = 1 + rangeEnd - rangeStart;

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
        /// Serves a partial range request where the total content length is not known
        /// </summary>
        private async Task ServePartialRangeRequestWithUnknownTotalContentLength(KeyValuePair<long, long?> requestedRange, Stream responseStream)
        {
            // Read the entire stream so that we can determine the length
            byte[] bytes = await ReadBytes(SourceStream, 0, null).ConfigureAwait(false);

            long totalContentLength = bytes.LongLength;

            long rangeStart = requestedRange.Key;
            long rangeEnd = requestedRange.Value ?? (totalContentLength - 1);
            long rangeLength = 1 + rangeEnd - rangeStart;

            // Content-Length is the length of what we're serving, not the original content
            HttpListenerContext.Response.ContentLength64 = rangeLength;
            HttpListenerContext.Response.Headers["Content-Range"] = string.Format("bytes {0}-{1}/{2}", rangeStart, rangeEnd, totalContentLength);

            await responseStream.WriteAsync(bytes, Convert.ToInt32(rangeStart), Convert.ToInt32(rangeLength)).ConfigureAwait(false);
        }

        /// <summary>
        /// Serves a partial range request where the total content length is already known
        /// </summary>
        private async Task ServePartialRangeRequestWithKnownTotalContentLength(KeyValuePair<long, long?> requestedRange, Stream responseStream)
        {
            long totalContentLength = TotalContentLength.Value;
            long rangeStart = requestedRange.Key;
            long rangeEnd = requestedRange.Value ?? (totalContentLength - 1);
            long rangeLength = 1 + rangeEnd - rangeStart;

            // Only read the bytes we need
            byte[] bytes = await ReadBytes(SourceStream, Convert.ToInt32(rangeStart), Convert.ToInt32(rangeLength)).ConfigureAwait(false);

            // Content-Length is the length of what we're serving, not the original content
            HttpListenerContext.Response.ContentLength64 = rangeLength;

            HttpListenerContext.Response.Headers["Content-Range"] = string.Format("bytes {0}-{1}/{2}", rangeStart, rangeEnd, totalContentLength);

            await responseStream.WriteAsync(bytes, 0, Convert.ToInt32(rangeLength)).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads bytes from a stream
        /// </summary>
        /// <param name="input">The input stream</param>
        /// <param name="start">The starting position</param>
        /// <param name="count">The number of bytes to read, or null to read to the end.</param>
        private async Task<byte[]> ReadBytes(Stream input, int start, int? count)
        {
            if (start > 0)
            {
                input.Position = start;
            }

            if (count == null)
            {
                var buffer = new byte[16 * 1024];

                using (var ms = new MemoryStream())
                {
                    int read;
                    while ((read = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                    {
                        await ms.WriteAsync(buffer, 0, read).ConfigureAwait(false);
                    }
                    return ms.ToArray();
                }
            }
            else
            {
                var buffer = new byte[count.Value];

                using (var ms = new MemoryStream())
                {
                    int read = await input.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false);

                    await ms.WriteAsync(buffer, 0, read).ConfigureAwait(false);

                    return ms.ToArray();
                }
            }

        }
    }
}

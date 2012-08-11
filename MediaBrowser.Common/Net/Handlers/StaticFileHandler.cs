using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using MediaBrowser.Common.Logging;

namespace MediaBrowser.Common.Net.Handlers
{
    public class StaticFileHandler : BaseHandler
    {
        private string _Path;
        public virtual string Path
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(_Path))
                {
                    return _Path;
                }

                return QueryString["path"];
            }
            set
            {
                _Path = value;
            }
        }

        private bool FileStreamDiscovered = false;
        private FileStream _FileStream = null;
        private FileStream FileStream
        {
            get
            {
                if (!FileStreamDiscovered)
                {
                    try
                    {
                        _FileStream = File.OpenRead(Path);
                    }
                    catch (FileNotFoundException)
                    {
                        StatusCode = 404;
                    }
                    catch (DirectoryNotFoundException)
                    {
                        StatusCode = 404;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        StatusCode = 403;
                    }
                    finally
                    {
                        FileStreamDiscovered = true;
                    }
                }

                return _FileStream;
            }
        }

        protected override bool SupportsByteRangeRequests
        {
            get
            {
                return true;
            }
        }

        public override bool CompressResponse
        {
            get
            {
                string contentType = ContentType;

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
        }

        protected override long? GetTotalContentLength()
        {
            try
            {
                return FileStream.Length;
            }
            catch
            {
                return base.GetTotalContentLength();
            }
        }

        protected override DateTime? GetLastDateModified()
        {
            try
            {
                return File.GetLastWriteTime(Path);
            }
            catch
            {
                return base.GetLastDateModified();
            }
        }

        protected override bool IsAsyncHandler
        {
            get
            {
                return true;
            }
        }

        public override string ContentType
        {
            get
            {
                return MimeTypes.GetMimeType(Path);
            }
        }

        protected async override void WriteResponseToOutputStream(Stream stream)
        {
            try
            {
                if (FileStream != null)
                {
                    if (IsRangeRequest)
                    {
                        KeyValuePair<long, long?> requestedRange = RequestedRanges.First();

                        // If the requested range is "0-" and we know the total length, we can optimize by avoiding having to buffer the content into memory
                        if (requestedRange.Value == null && TotalContentLength != null)
                        {
                            await ServeCompleteRangeRequest(requestedRange, stream);
                        }
                        else if (TotalContentLength.HasValue)
                        {
                            // This will have to buffer a portion of the content into memory
                            await ServePartialRangeRequestWithKnownTotalContentLength(requestedRange, stream);
                        }
                        else
                        {
                            // This will have to buffer the entire content into memory
                            await ServePartialRangeRequestWithUnknownTotalContentLength(requestedRange, stream);
                        }
                    }
                    else
                    {
                        await FileStream.CopyToAsync(stream);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException("WriteResponseToOutputStream", ex);
            }
            finally
            {
                if (FileStream != null)
                {
                    FileStream.Dispose();
                }

                DisposeResponseStream();
            }
        }

        /// <summary>
        /// Handles a range request of "bytes=0-"
        /// This will serve the complete content and add the content-range header
        /// </summary>
        private async Task ServeCompleteRangeRequest(KeyValuePair<long, long?> requestedRange, Stream responseStream)
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
                FileStream.Position = rangeStart;
            }

            await FileStream.CopyToAsync(responseStream);
        }

        /// <summary>
        /// Serves a partial range request where the total content length is not known
        /// </summary>
        private async Task ServePartialRangeRequestWithUnknownTotalContentLength(KeyValuePair<long, long?> requestedRange, Stream responseStream)
        {
            // Read the entire stream so that we can determine the length
            byte[] bytes = await ReadBytes(FileStream, 0, null);

            long totalContentLength = bytes.LongLength;

            long rangeStart = requestedRange.Key;
            long rangeEnd = requestedRange.Value ?? (totalContentLength - 1);
            long rangeLength = 1 + rangeEnd - rangeStart;

            // Content-Length is the length of what we're serving, not the original content
            HttpListenerContext.Response.ContentLength64 = rangeLength;
            HttpListenerContext.Response.Headers["Content-Range"] = string.Format("bytes {0}-{1}/{2}", rangeStart, rangeEnd, totalContentLength);

            await responseStream.WriteAsync(bytes, Convert.ToInt32(rangeStart), Convert.ToInt32(rangeLength));
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
            byte[] bytes = await ReadBytes(FileStream, Convert.ToInt32(rangeStart), Convert.ToInt32(rangeLength));

            // Content-Length is the length of what we're serving, not the original content
            HttpListenerContext.Response.ContentLength64 = rangeLength;

            HttpListenerContext.Response.Headers["Content-Range"] = string.Format("bytes {0}-{1}/{2}", rangeStart, rangeEnd, totalContentLength);

            await responseStream.WriteAsync(bytes, 0, Convert.ToInt32(rangeLength));
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
                byte[] buffer = new byte[16 * 1024];

                using (MemoryStream ms = new MemoryStream())
                {
                    int read;
                    while ((read = await input.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await ms.WriteAsync(buffer, 0, read);
                    }
                    return ms.ToArray();
                }
            }
            else
            {
                byte[] buffer = new byte[count.Value];

                using (MemoryStream ms = new MemoryStream())
                {
                    int read = await input.ReadAsync(buffer, 0, buffer.Length);

                    await ms.WriteAsync(buffer, 0, read);

                    return ms.ToArray();
                }
            }

        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using MediaBrowser.Common.Logging;

namespace MediaBrowser.Common.Net.Handlers
{
    public abstract class BaseHandler
    {
        private Stream CompressedStream { get; set; }

        public virtual bool? UseChunkedEncoding
        {
            get
            {
                return null;
            }
        }

        private bool _TotalContentLengthDiscovered = false;
        private long? _TotalContentLength = null;
        public long? TotalContentLength
        {
            get
            {
                if (!_TotalContentLengthDiscovered)
                {
                    _TotalContentLength = GetTotalContentLength();
                }

                return _TotalContentLength;
            }
        }

        /// <summary>
        /// Returns true or false indicating if the handler writes to the stream asynchronously.
        /// If so the subclass will be responsible for disposing the stream when complete.
        /// </summary>
        protected virtual bool IsAsyncHandler
        {
            get
            {
                return false;
            }
        }

        protected virtual bool SupportsByteRangeRequests
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// The original HttpListenerContext
        /// </summary>
        protected HttpListenerContext HttpListenerContext { get; set; }

        /// <summary>
        /// The original QueryString
        /// </summary>
        protected NameValueCollection QueryString
        {
            get
            {
                return HttpListenerContext.Request.QueryString;
            }
        }

        protected List<KeyValuePair<long, long?>> _RequestedRanges = null;
        protected IEnumerable<KeyValuePair<long, long?>> RequestedRanges
        {
            get
            {
                if (_RequestedRanges == null)
                {
                    _RequestedRanges = new List<KeyValuePair<long, long?>>();

                    if (IsRangeRequest)
                    {
                        // Example: bytes=0-,32-63
                        string[] ranges = HttpListenerContext.Request.Headers["Range"].Split('=')[1].Split(',');

                        foreach (string range in ranges)
                        {
                            string[] vals = range.Split('-');

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

                            _RequestedRanges.Add(new KeyValuePair<long, long?>(start, end));
                        }
                    }
                }

                return _RequestedRanges;
            }
        }

        protected bool IsRangeRequest
        {
            get
            {
                return HttpListenerContext.Request.Headers.AllKeys.Contains("Range");
            }
        }

        /// <summary>
        /// Gets the MIME type to include in the response headers
        /// </summary>
        public abstract string ContentType { get; }

        /// <summary>
        /// Gets the status code to include in the response headers
        /// </summary>
        protected int StatusCode { get; set; }

        /// <summary>
        /// Gets the cache duration to include in the response headers
        /// </summary>
        public virtual TimeSpan CacheDuration
        {
            get
            {
                return TimeSpan.FromTicks(0);
            }
        }

        private bool _LastDateModifiedDiscovered = false;
        private DateTime? _LastDateModified = null;
        /// <summary>
        /// Gets the last date modified of the content being returned, if this can be determined.
        /// This will be used to invalidate the cache, so it's not needed if CacheDuration is 0.
        /// </summary>
        public DateTime? LastDateModified
        {
            get
            {
                if (!_LastDateModifiedDiscovered)
                {
                    _LastDateModified = GetLastDateModified();
                }

                return _LastDateModified;
            }
        }
        
        public virtual bool CompressResponse
        {
            get
            {
                return true;
            }
        }

        private bool ClientSupportsCompression
        {
            get
            {
                string enc = HttpListenerContext.Request.Headers["Accept-Encoding"] ?? string.Empty;

                return enc.IndexOf("deflate", StringComparison.OrdinalIgnoreCase) != -1 || enc.IndexOf("gzip", StringComparison.OrdinalIgnoreCase) != -1;
            }
        }

        private string CompressionMethod
        {
            get
            {
                string enc = HttpListenerContext.Request.Headers["Accept-Encoding"] ?? string.Empty;

                if (enc.IndexOf("deflate", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return "deflate";
                }
                if (enc.IndexOf("gzip", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    return "gzip";
                }

                return null;
            }
        }

        public virtual void ProcessRequest(HttpListenerContext ctx)
        {
            HttpListenerContext = ctx;

            Logger.LogInfo("Http Server received request at: " + ctx.Request.Url.ToString());
            Logger.LogInfo("Http Headers: " + string.Join(",", ctx.Request.Headers.AllKeys.Select(k => k + "=" + ctx.Request.Headers[k])));

            ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");

            ctx.Response.KeepAlive = true;

            if (SupportsByteRangeRequests && IsRangeRequest)
            {
                ctx.Response.Headers["Accept-Ranges"] = "bytes";
            }
            
            // Set the initial status code
            // When serving a range request, we need to return status code 206 to indicate a partial response body
            StatusCode = SupportsByteRangeRequests && IsRangeRequest ? 206 : 200;

            ctx.Response.ContentType = ContentType;

            TimeSpan cacheDuration = CacheDuration;

            if (ctx.Request.Headers.AllKeys.Contains("If-Modified-Since"))
            {
                DateTime ifModifiedSince;

                if (DateTime.TryParse(ctx.Request.Headers["If-Modified-Since"].Replace(" GMT", string.Empty), out ifModifiedSince))
                {
                    // If the cache hasn't expired yet just return a 304
                    if (IsCacheValid(ifModifiedSince, cacheDuration, LastDateModified))
                    {
                        StatusCode = 304;
                    }
                }
            }

            if (StatusCode == 200 || StatusCode == 206)
            {
                ProcessUncachedResponse(ctx, cacheDuration);
            }
            else
            {
                ctx.Response.StatusCode = StatusCode;
                ctx.Response.SendChunked = false;
                DisposeResponseStream();
            }
        }

        private void ProcessUncachedResponse(HttpListenerContext ctx, TimeSpan cacheDuration)
        {
            long? totalContentLength = TotalContentLength;

            // By default, use chunked encoding if we don't know the content length
            bool useChunkedEncoding = UseChunkedEncoding == null ? (totalContentLength == null) : UseChunkedEncoding.Value;

            // Don't force this to true. HttpListener will default it to true if supported by the client.
            if (!useChunkedEncoding)
            {
                ctx.Response.SendChunked = false;
            }

            // Set the content length, if we know it
            if (totalContentLength.HasValue)
            {
                ctx.Response.ContentLength64 = totalContentLength.Value;
            }

            // Add the compression header
            if (CompressResponse && ClientSupportsCompression)
            {
                ctx.Response.AddHeader("Content-Encoding", CompressionMethod);
            }

            // Add caching headers
            if (cacheDuration.Ticks > 0)
            {
                CacheResponse(ctx.Response, cacheDuration, LastDateModified);
            }

            PrepareUncachedResponse(ctx, cacheDuration);

            // Set the status code
            ctx.Response.StatusCode = StatusCode;

            if (StatusCode == 200 || StatusCode == 206)
            {
                // Finally, write the response data
                Stream outputStream = ctx.Response.OutputStream;

                if (CompressResponse && ClientSupportsCompression)
                {
                    if (CompressionMethod.Equals("deflate", StringComparison.OrdinalIgnoreCase))
                    {
                        CompressedStream = new DeflateStream(outputStream, CompressionLevel.Fastest, false);
                    }
                    else
                    {
                        CompressedStream = new GZipStream(outputStream, CompressionLevel.Fastest, false);
                    }

                    outputStream = CompressedStream;
                }

                WriteResponseToOutputStream(outputStream);

                if (!IsAsyncHandler)
                {
                    DisposeResponseStream();
                }
            }
            else
            {
                ctx.Response.SendChunked = false;
                DisposeResponseStream();
            }
        }

        protected virtual void PrepareUncachedResponse(HttpListenerContext ctx, TimeSpan cacheDuration)
        {
        }

        private void CacheResponse(HttpListenerResponse response, TimeSpan duration, DateTime? dateModified)
        {
            DateTime lastModified = dateModified ?? DateTime.Now;

            response.Headers[HttpResponseHeader.CacheControl] = "public, max-age=" + Convert.ToInt32(duration.TotalSeconds);
            response.Headers[HttpResponseHeader.Expires] = DateTime.Now.Add(duration).ToString("r");
            response.Headers[HttpResponseHeader.LastModified] = lastModified.ToString("r");
        }

        protected abstract void WriteResponseToOutputStream(Stream stream);

        protected void DisposeResponseStream()
        {
            if (CompressedStream != null)
            {
                CompressedStream.Dispose();
            }

            HttpListenerContext.Response.OutputStream.Dispose();
        }

        private bool IsCacheValid(DateTime ifModifiedSince, TimeSpan cacheDuration, DateTime? dateModified)
        {
            if (dateModified.HasValue)
            {
                DateTime lastModified = NormalizeDateForComparison(dateModified.Value);
                ifModifiedSince = NormalizeDateForComparison(ifModifiedSince);

                return lastModified <= ifModifiedSince;
            }

            DateTime cacheExpirationDate = ifModifiedSince.Add(cacheDuration);

            if (DateTime.Now < cacheExpirationDate)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// When the browser sends the IfModifiedDate, it's precision is limited to seconds, so this will account for that
        /// </summary>
        private DateTime NormalizeDateForComparison(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second);
        }

        protected virtual long? GetTotalContentLength()
        {
            return null;
        }

        protected virtual DateTime? GetLastDateModified()
        {
            return null;
        }
    }
}
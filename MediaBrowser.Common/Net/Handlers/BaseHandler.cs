using MediaBrowser.Common.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net.Handlers
{
    public abstract class BaseHandler
    {
        public abstract bool HandlesRequest(HttpListenerRequest request);

        private Stream CompressedStream { get; set; }

        public virtual bool? UseChunkedEncoding
        {
            get
            {
                return null;
            }
        }

        private bool _TotalContentLengthDiscovered;
        private long? _TotalContentLength;
        public long? TotalContentLength
        {
            get
            {
                if (!_TotalContentLengthDiscovered)
                {
                    _TotalContentLength = GetTotalContentLength();
                    _TotalContentLengthDiscovered = true;
                }

                return _TotalContentLength;
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

        private List<KeyValuePair<long, long?>> _RequestedRanges;
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
        public abstract Task<string> GetContentType();

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

        public virtual bool ShouldCompressResponse(string contentType)
        {
            return true;
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

        public virtual async Task ProcessRequest(HttpListenerContext ctx)
        {
            HttpListenerContext = ctx;

            string url = ctx.Request.Url.ToString();
            Logger.LogInfo("Http Server received request at: " + url);
            Logger.LogInfo("Http Headers: " + string.Join(",", ctx.Request.Headers.AllKeys.Select(k => k + "=" + ctx.Request.Headers[k])));

            ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");

            ctx.Response.KeepAlive = true;

            try
            {
                if (SupportsByteRangeRequests && IsRangeRequest)
                {
                    ctx.Response.Headers["Accept-Ranges"] = "bytes";
                }

                // Set the initial status code
                // When serving a range request, we need to return status code 206 to indicate a partial response body
                StatusCode = SupportsByteRangeRequests && IsRangeRequest ? 206 : 200;

                ctx.Response.ContentType = await GetContentType().ConfigureAwait(false);

                TimeSpan cacheDuration = CacheDuration;

                DateTime? lastDateModified = await GetLastDateModified().ConfigureAwait(false);

                if (ctx.Request.Headers.AllKeys.Contains("If-Modified-Since"))
                {
                    DateTime ifModifiedSince;

                    if (DateTime.TryParse(ctx.Request.Headers["If-Modified-Since"], out ifModifiedSince))
                    {
                        // If the cache hasn't expired yet just return a 304
                        if (IsCacheValid(ifModifiedSince.ToUniversalTime(), cacheDuration, lastDateModified))
                        {
                            StatusCode = 304;
                        }
                    }
                }

                await PrepareResponse().ConfigureAwait(false);

                Logger.LogInfo("Responding with status code {0} for url {1}", StatusCode, url);

                if (IsResponseValid)
                {
                    bool compressResponse = ShouldCompressResponse(ctx.Response.ContentType) && ClientSupportsCompression;

                    await ProcessUncachedRequest(ctx, compressResponse, cacheDuration, lastDateModified).ConfigureAwait(false);
                }
                else
                {
                    ctx.Response.StatusCode = StatusCode;
                    ctx.Response.SendChunked = false;
                }
            }
            catch (Exception ex)
            {
                // It might be too late if some response data has already been transmitted, but try to set this
                ctx.Response.StatusCode = 500;

                Logger.LogException(ex);
            }
            finally
            {
                DisposeResponseStream();
            }
        }

        private async Task ProcessUncachedRequest(HttpListenerContext ctx, bool compressResponse, TimeSpan cacheDuration, DateTime? lastDateModified)
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
            if (compressResponse)
            {
                ctx.Response.AddHeader("Content-Encoding", CompressionMethod);
            }

            // Add caching headers
            if (cacheDuration.Ticks > 0)
            {
                CacheResponse(ctx.Response, cacheDuration, lastDateModified);
            }

            // Set the status code
            ctx.Response.StatusCode = StatusCode;

            if (IsResponseValid)
            {
                // Finally, write the response data
                Stream outputStream = ctx.Response.OutputStream;

                if (compressResponse)
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

                await WriteResponseToOutputStream(outputStream).ConfigureAwait(false);
            }
            else
            {
                ctx.Response.SendChunked = false;
            }
        }

        private void CacheResponse(HttpListenerResponse response, TimeSpan duration, DateTime? dateModified)
        {
            DateTime now = DateTime.UtcNow;

            DateTime lastModified = dateModified ?? now;

            response.Headers[HttpResponseHeader.CacheControl] = "public, max-age=" + Convert.ToInt32(duration.TotalSeconds);
            response.Headers[HttpResponseHeader.Expires] = now.Add(duration).ToString("r");
            response.Headers[HttpResponseHeader.LastModified] = lastModified.ToString("r");
        }

        /// <summary>
        /// Gives subclasses a chance to do any prep work, and also to validate data and set an error status code, if needed
        /// </summary>
        protected virtual Task PrepareResponse()
        {
            return Task.FromResult<object>(null);
        }

        protected abstract Task WriteResponseToOutputStream(Stream stream);

        protected virtual void DisposeResponseStream()
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

            if (DateTime.UtcNow < cacheExpirationDate)
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
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind);
        }

        protected virtual long? GetTotalContentLength()
        {
            return null;
        }

        protected virtual Task<DateTime?> GetLastDateModified()
        {
            DateTime? value = null;

            return Task.FromResult(value);
        }

        private bool IsResponseValid
        {
            get
            {
                return StatusCode == 200 || StatusCode == 206;
            }
        }

        private Hashtable _FormValues;

        /// <summary>
        /// Gets a value from form POST data
        /// </summary>
        protected async Task<string> GetFormValue(string name)
        {
            if (_FormValues == null)
            {
                _FormValues = await GetFormValues(HttpListenerContext.Request).ConfigureAwait(false);
            }

            if (_FormValues.ContainsKey(name))
            {
                return _FormValues[name].ToString();
            }

            return null;
        }

        /// <summary>
        /// Extracts form POST data from a request
        /// </summary>
        private async Task<Hashtable> GetFormValues(HttpListenerRequest request)
        {
            Hashtable formVars = new Hashtable();

            if (request.HasEntityBody)
            {
                if (request.ContentType.IndexOf("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    using (Stream requestBody = request.InputStream)
                    {
                        using (StreamReader reader = new StreamReader(requestBody, request.ContentEncoding))
                        {
                            string s = await reader.ReadToEndAsync().ConfigureAwait(false);

                            string[] pairs = s.Split('&');

                            for (int x = 0; x < pairs.Length; x++)
                            {
                                string pair = pairs[x];

                                int index = pair.IndexOf('=');

                                if (index != -1)
                                {
                                    string name = pair.Substring(0, index);
                                    string value = pair.Substring(index + 1);
                                    formVars.Add(name, value);
                                }
                            }
                        }
                    }
                }
            }

            return formVars;
        }
    }
}
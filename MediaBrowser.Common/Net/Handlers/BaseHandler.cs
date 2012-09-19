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

        private bool _totalContentLengthDiscovered;
        private long? _totalContentLength;
        public long? TotalContentLength
        {
            get
            {
                if (!_totalContentLengthDiscovered)
                {
                    _totalContentLength = GetTotalContentLength();
                    _totalContentLengthDiscovered = true;
                }

                return _totalContentLength;
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

        private List<KeyValuePair<long, long?>> _requestedRanges;
        protected IEnumerable<KeyValuePair<long, long?>> RequestedRanges
        {
            get
            {
                if (_requestedRanges == null)
                {
                    _requestedRanges = new List<KeyValuePair<long, long?>>();

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

                            _requestedRanges.Add(new KeyValuePair<long, long?>(start, end));
                        }
                    }
                }

                return _requestedRanges;
            }
        }

        protected bool IsRangeRequest
        {
            get
            {
                return HttpListenerContext.Request.Headers.AllKeys.Contains("Range");
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

                ResponseInfo responseInfo = await GetResponseInfo().ConfigureAwait(false);

                if (responseInfo.IsResponseValid)
                {
                    // Set the initial status code
                    // When serving a range request, we need to return status code 206 to indicate a partial response body
                    responseInfo.StatusCode = SupportsByteRangeRequests && IsRangeRequest ? 206 : 200;
                }

                ctx.Response.ContentType = responseInfo.ContentType;

                if (!string.IsNullOrEmpty(responseInfo.Etag))
                {
                    ctx.Response.Headers["ETag"] = responseInfo.Etag;
                }

                if (ctx.Request.Headers.AllKeys.Contains("If-Modified-Since"))
                {
                    DateTime ifModifiedSince;

                    if (DateTime.TryParse(ctx.Request.Headers["If-Modified-Since"], out ifModifiedSince))
                    {
                        // If the cache hasn't expired yet just return a 304
                        if (IsCacheValid(ifModifiedSince.ToUniversalTime(), responseInfo.CacheDuration, responseInfo.DateLastModified))
                        {
                            // ETag must also match (if supplied)
                            if ((responseInfo.Etag ?? string.Empty).Equals(ctx.Request.Headers["If-None-Match"] ?? string.Empty))
                            {
                                responseInfo.StatusCode = 304;
                            }
                        }
                    }
                }

                Logger.LogInfo("Responding with status code {0} for url {1}", responseInfo.StatusCode, url);

                if (responseInfo.IsResponseValid)
                {
                    await ProcessUncachedRequest(ctx, responseInfo).ConfigureAwait(false);
                }
                else
                {
                    ctx.Response.StatusCode = responseInfo.StatusCode;
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

        private async Task ProcessUncachedRequest(HttpListenerContext ctx, ResponseInfo responseInfo)
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

            var compressResponse = responseInfo.CompressResponse && ClientSupportsCompression;

            // Add the compression header
            if (compressResponse)
            {
                ctx.Response.AddHeader("Content-Encoding", CompressionMethod);
            }

            if (responseInfo.DateLastModified.HasValue)
            {
                ctx.Response.Headers[HttpResponseHeader.LastModified] = responseInfo.DateLastModified.Value.ToString("r");
            }

            // Add caching headers
            if (responseInfo.CacheDuration.Ticks > 0)
            {
                CacheResponse(ctx.Response, responseInfo.CacheDuration);
            }

            // Set the status code
            ctx.Response.StatusCode = responseInfo.StatusCode;

            if (responseInfo.IsResponseValid)
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

        private void CacheResponse(HttpListenerResponse response, TimeSpan duration)
        {
            response.Headers[HttpResponseHeader.CacheControl] = "public, max-age=" + Convert.ToInt32(duration.TotalSeconds);
            response.Headers[HttpResponseHeader.Expires] = DateTime.UtcNow.Add(duration).ToString("r");
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

        protected abstract Task<ResponseInfo> GetResponseInfo();

        private Hashtable _formValues;

        /// <summary>
        /// Gets a value from form POST data
        /// </summary>
        protected async Task<string> GetFormValue(string name)
        {
            if (_formValues == null)
            {
                _formValues = await GetFormValues(HttpListenerContext.Request).ConfigureAwait(false);
            }

            if (_formValues.ContainsKey(name))
            {
                return _formValues[name].ToString();
            }

            return null;
        }

        /// <summary>
        /// Extracts form POST data from a request
        /// </summary>
        private async Task<Hashtable> GetFormValues(HttpListenerRequest request)
        {
            var formVars = new Hashtable();

            if (request.HasEntityBody)
            {
                if (request.ContentType.IndexOf("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    using (Stream requestBody = request.InputStream)
                    {
                        using (var reader = new StreamReader(requestBody, request.ContentEncoding))
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

    public class ResponseInfo
    {
        public string ContentType { get; set; }
        public string Etag { get; set; }
        public DateTime? DateLastModified { get; set; }
        public TimeSpan CacheDuration { get; set; }
        public bool CompressResponse { get; set; }
        public int StatusCode { get; set; }

        public ResponseInfo()
        {
            CacheDuration = TimeSpan.FromTicks(0);

            CompressResponse = true;

            StatusCode = 200;
        }

        public bool IsResponseValid
        {
            get
            {
                return StatusCode == 200 || StatusCode == 206;
            }
        }
    }
}
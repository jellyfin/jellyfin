using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Kernel;
using MediaBrowser.Common.Logging;
using MediaBrowser.Model.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace MediaBrowser.Common.Net.Handlers
{
    /// <summary>
    /// Class BaseHandler
    /// </summary>
    public abstract class BaseHandler<TKernelType> : IHttpServerHandler
        where TKernelType : IKernel
    {
        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// Initializes the specified kernel.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        public void Initialize(IKernel kernel)
        {
            Kernel = (TKernelType)kernel;
            Logger = SharedLogger.Logger;
        }

        /// <summary>
        /// Gets or sets the kernel.
        /// </summary>
        /// <value>The kernel.</value>
        protected TKernelType Kernel { get; private set; }

        /// <summary>
        /// Gets the URL suffix used to determine if this handler can process a request.
        /// </summary>
        /// <value>The URL suffix.</value>
        protected virtual string UrlSuffix
        {
            get
            {
                var name = GetType().Name;

                const string srch = "Handler";

                if (name.EndsWith(srch, StringComparison.OrdinalIgnoreCase))
                {
                    name = name.Substring(0, name.Length - srch.Length);
                }

                return "api/" + name;
            }
        }

        /// <summary>
        /// Handleses the request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public virtual bool HandlesRequest(HttpListenerRequest request)
        {
            var name = '/' + UrlSuffix.TrimStart('/');

            var url = Kernel.WebApplicationName + name;

            return request.Url.LocalPath.EndsWith(url, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Gets or sets the compressed stream.
        /// </summary>
        /// <value>The compressed stream.</value>
        private Stream CompressedStream { get; set; }

        /// <summary>
        /// Gets a value indicating whether [use chunked encoding].
        /// </summary>
        /// <value><c>null</c> if [use chunked encoding] contains no value, <c>true</c> if [use chunked encoding]; otherwise, <c>false</c>.</value>
        public virtual bool? UseChunkedEncoding
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// The original HttpListenerContext
        /// </summary>
        /// <value>The HTTP listener context.</value>
        protected HttpListenerContext HttpListenerContext { get; set; }

        /// <summary>
        /// The _query string
        /// </summary>
        private NameValueCollection _queryString;
        /// <summary>
        /// The original QueryString
        /// </summary>
        /// <value>The query string.</value>
        public NameValueCollection QueryString
        {
            get
            {
                // HttpListenerContext.Request.QueryString is not decoded properly
                return _queryString ?? (_queryString = HttpUtility.ParseQueryString(HttpListenerContext.Request.Url.Query));
            }
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

                    if (IsRangeRequest)
                    {
                        // Example: bytes=0-,32-63
                        var ranges = HttpListenerContext.Request.Headers["Range"].Split('=')[1].Split(',');

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
                }

                return _requestedRanges;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is range request.
        /// </summary>
        /// <value><c>true</c> if this instance is range request; otherwise, <c>false</c>.</value>
        protected bool IsRangeRequest
        {
            get
            {
                return HttpListenerContext.Request.Headers.AllKeys.Contains("Range");
            }
        }

        /// <summary>
        /// Gets a value indicating whether [client supports compression].
        /// </summary>
        /// <value><c>true</c> if [client supports compression]; otherwise, <c>false</c>.</value>
        protected bool ClientSupportsCompression
        {
            get
            {
                var enc = HttpListenerContext.Request.Headers["Accept-Encoding"] ?? string.Empty;

                return enc.Equals("*", StringComparison.OrdinalIgnoreCase) ||
                    enc.IndexOf("deflate", StringComparison.OrdinalIgnoreCase) != -1 ||
                    enc.IndexOf("gzip", StringComparison.OrdinalIgnoreCase) != -1;
            }
        }

        /// <summary>
        /// Gets the compression method.
        /// </summary>
        /// <value>The compression method.</value>
        private string CompressionMethod
        {
            get
            {
                var enc = HttpListenerContext.Request.Headers["Accept-Encoding"] ?? string.Empty;

                if (enc.IndexOf("deflate", StringComparison.OrdinalIgnoreCase) != -1 || enc.Equals("*", StringComparison.OrdinalIgnoreCase))
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

        /// <summary>
        /// Processes the request.
        /// </summary>
        /// <param name="ctx">The CTX.</param>
        /// <returns>Task.</returns>
        public virtual async Task ProcessRequest(HttpListenerContext ctx)
        {
            HttpListenerContext = ctx;

            ctx.Response.AddHeader("Access-Control-Allow-Origin", "*");

            ctx.Response.KeepAlive = true;

            try
            {
                await ProcessRequestInternal(ctx).ConfigureAwait(false);
            }
            catch (InvalidOperationException ex)
            {
                HandleException(ctx.Response, ex, 422);

                throw;
            }
            catch (ResourceNotFoundException ex)
            {
                HandleException(ctx.Response, ex, 404);

                throw;
            }
            catch (FileNotFoundException ex)
            {
                HandleException(ctx.Response, ex, 404);

                throw;
            }
            catch (DirectoryNotFoundException ex)
            {
                HandleException(ctx.Response, ex, 404);

                throw;
            }
            catch (UnauthorizedAccessException ex)
            {
                HandleException(ctx.Response, ex, 401);

                throw;
            }
            catch (ArgumentException ex)
            {
                HandleException(ctx.Response, ex, 400);

                throw;
            }
            catch (Exception ex)
            {
                HandleException(ctx.Response, ex, 500);

                throw;
            }
            finally
            {
                DisposeResponseStream();
            }
        }

        /// <summary>
        /// Appends the error message.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="ex">The ex.</param>
        /// <param name="statusCode">The status code.</param>
        private void HandleException(HttpListenerResponse response, Exception ex, int statusCode)
        {
            response.StatusCode = statusCode;

            response.Headers.Add("Status", statusCode.ToString(new CultureInfo("en-US")));

            response.Headers.Remove("Age");
            response.Headers.Remove("Expires");
            response.Headers.Remove("Cache-Control");
            response.Headers.Remove("Etag");
            response.Headers.Remove("Last-Modified");

            response.ContentType = "text/plain";

            Logger.ErrorException("Error processing request", ex);
            
            if (!string.IsNullOrEmpty(ex.Message))
            {
                response.AddHeader("X-Application-Error-Code", ex.Message);
            }

            var bytes = Encoding.UTF8.GetBytes(ex.Message);

            var stream = CompressedStream ?? response.OutputStream;

            // This could fail, but try to add the stack trace as the body content
            try
            {
                stream.Write(bytes, 0, bytes.Length);
            }
            catch (Exception ex1)
            {
                Logger.ErrorException("Error dumping stack trace", ex1);
            }
        }

        /// <summary>
        /// Processes the request internal.
        /// </summary>
        /// <param name="ctx">The CTX.</param>
        /// <returns>Task.</returns>
        private async Task ProcessRequestInternal(HttpListenerContext ctx)
        {
            var responseInfo = await GetResponseInfo().ConfigureAwait(false);

            // Let the client know if byte range requests are supported or not
            if (responseInfo.SupportsByteRangeRequests)
            {
                ctx.Response.Headers["Accept-Ranges"] = "bytes";
            }
            else if (!responseInfo.SupportsByteRangeRequests)
            {
                ctx.Response.Headers["Accept-Ranges"] = "none";
            }

            if (responseInfo.IsResponseValid && responseInfo.SupportsByteRangeRequests && IsRangeRequest)
            {
                // Set the initial status code
                // When serving a range request, we need to return status code 206 to indicate a partial response body
                responseInfo.StatusCode = 206;
            }

            ctx.Response.ContentType = responseInfo.ContentType;

            if (responseInfo.Etag.HasValue)
            {
                ctx.Response.Headers["ETag"] = responseInfo.Etag.Value.ToString("N");
            }

            var isCacheValid = true;

            // Validate If-Modified-Since
            if (ctx.Request.Headers.AllKeys.Contains("If-Modified-Since"))
            {
                DateTime ifModifiedSince;

                if (DateTime.TryParse(ctx.Request.Headers["If-Modified-Since"], out ifModifiedSince))
                {
                    isCacheValid = IsCacheValid(ifModifiedSince.ToUniversalTime(), responseInfo.CacheDuration,
                                                responseInfo.DateLastModified);
                }
            }

            // Validate If-None-Match
            if (isCacheValid &&
                (responseInfo.Etag.HasValue || !string.IsNullOrEmpty(ctx.Request.Headers["If-None-Match"])))
            {
                Guid ifNoneMatch;

                if (Guid.TryParse(ctx.Request.Headers["If-None-Match"] ?? string.Empty, out ifNoneMatch))
                {
                    if (responseInfo.Etag.HasValue && responseInfo.Etag.Value == ifNoneMatch)
                    {
                        responseInfo.StatusCode = 304;
                    }
                }
            }

            LogResponse(ctx, responseInfo);

            if (responseInfo.IsResponseValid)
            {
                await OnProcessingRequest(responseInfo).ConfigureAwait(false);
            }

            if (responseInfo.IsResponseValid)
            {
                await ProcessUncachedRequest(ctx, responseInfo).ConfigureAwait(false);
            }
            else
            {
                if (responseInfo.StatusCode == 304)
                {
                    AddAgeHeader(ctx.Response, responseInfo);
                    AddExpiresHeader(ctx.Response, responseInfo);
                }

                ctx.Response.StatusCode = responseInfo.StatusCode;
                ctx.Response.SendChunked = false;
            }
        }

        /// <summary>
        /// The _null task result
        /// </summary>
        private readonly Task<bool> _nullTaskResult = Task.FromResult(true);

        /// <summary>
        /// Called when [processing request].
        /// </summary>
        /// <param name="responseInfo">The response info.</param>
        /// <returns>Task.</returns>
        protected virtual Task OnProcessingRequest(ResponseInfo responseInfo)
        {
            return _nullTaskResult;
        }

        /// <summary>
        /// Logs the response.
        /// </summary>
        /// <param name="ctx">The CTX.</param>
        /// <param name="responseInfo">The response info.</param>
        private void LogResponse(HttpListenerContext ctx, ResponseInfo responseInfo)
        {
            // Don't log normal 200's
            if (responseInfo.StatusCode == 200)
            {
                return;
            }

            var log = new StringBuilder();

            log.AppendLine(string.Format("Url: {0}", ctx.Request.Url));

            log.AppendLine("Headers: " + string.Join(",", ctx.Response.Headers.AllKeys.Select(k => k + "=" + ctx.Response.Headers[k])));

            var msg = "Http Response Sent (" + responseInfo.StatusCode + ") to " + ctx.Request.RemoteEndPoint;

            if (Kernel.Configuration.EnableHttpLevelLogging)
            {
                Logger.LogMultiline(msg, LogSeverity.Debug, log);
            }
        }

        /// <summary>
        /// Processes the uncached request.
        /// </summary>
        /// <param name="ctx">The CTX.</param>
        /// <param name="responseInfo">The response info.</param>
        /// <returns>Task.</returns>
        private async Task ProcessUncachedRequest(HttpListenerContext ctx, ResponseInfo responseInfo)
        {
            var totalContentLength = GetTotalContentLength(responseInfo);

            // By default, use chunked encoding if we don't know the content length
            var useChunkedEncoding = UseChunkedEncoding == null ? (totalContentLength == null) : UseChunkedEncoding.Value;

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
                ctx.Response.AddHeader("Vary", "Accept-Encoding");
            }

            // Don't specify both last modified and Etag, unless caching unconditionally. They are redundant
            // https://developers.google.com/speed/docs/best-practices/caching#LeverageBrowserCaching
            if (responseInfo.DateLastModified.HasValue && (!responseInfo.Etag.HasValue || responseInfo.CacheDuration.Ticks > 0))
            {
                ctx.Response.Headers[HttpResponseHeader.LastModified] = responseInfo.DateLastModified.Value.ToString("r");
                AddAgeHeader(ctx.Response, responseInfo);
            }

            // Add caching headers
            ConfigureCaching(ctx.Response, responseInfo);

            // Set the status code
            ctx.Response.StatusCode = responseInfo.StatusCode;

            if (responseInfo.IsResponseValid)
            {
                // Finally, write the response data
                var outputStream = ctx.Response.OutputStream;

                if (compressResponse)
                {
                    if (CompressionMethod.Equals("deflate", StringComparison.OrdinalIgnoreCase))
                    {
                        CompressedStream = new DeflateStream(outputStream, CompressionLevel.Fastest, true);
                    }
                    else
                    {
                        CompressedStream = new GZipStream(outputStream, CompressionLevel.Fastest, true);
                    }

                    outputStream = CompressedStream;
                }

                await WriteResponseToOutputStream(outputStream, responseInfo, totalContentLength).ConfigureAwait(false);
            }
            else
            {
                ctx.Response.SendChunked = false;
            }
        }

        /// <summary>
        /// Configures the caching.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="responseInfo">The response info.</param>
        private void ConfigureCaching(HttpListenerResponse response, ResponseInfo responseInfo)
        {
            if (responseInfo.CacheDuration.Ticks > 0)
            {
                response.Headers[HttpResponseHeader.CacheControl] = "public, max-age=" + Convert.ToInt32(responseInfo.CacheDuration.TotalSeconds);
            }
            else if (responseInfo.Etag.HasValue)
            {
                response.Headers[HttpResponseHeader.CacheControl] = "public";
            }
            else
            {
                response.Headers[HttpResponseHeader.CacheControl] = "no-cache, no-store, must-revalidate";
                response.Headers[HttpResponseHeader.Pragma] = "no-cache, no-store, must-revalidate";
            }

            AddExpiresHeader(response, responseInfo);
        }

        /// <summary>
        /// Adds the expires header.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="responseInfo">The response info.</param>
        private void AddExpiresHeader(HttpListenerResponse response, ResponseInfo responseInfo)
        {
            if (responseInfo.CacheDuration.Ticks > 0)
            {
                response.Headers[HttpResponseHeader.Expires] = DateTime.UtcNow.Add(responseInfo.CacheDuration).ToString("r");
            }
            else if (!responseInfo.Etag.HasValue)
            {
                response.Headers[HttpResponseHeader.Expires] = "-1";
            }
        }

        /// <summary>
        /// Adds the age header.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="responseInfo">The response info.</param>
        private void AddAgeHeader(HttpListenerResponse response, ResponseInfo responseInfo)
        {
            if (responseInfo.DateLastModified.HasValue)
            {
                response.Headers[HttpResponseHeader.Age] = Convert.ToInt32((DateTime.UtcNow - responseInfo.DateLastModified.Value).TotalSeconds).ToString(CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Writes the response to output stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="responseInfo">The response info.</param>
        /// <param name="contentLength">Length of the content.</param>
        /// <returns>Task.</returns>
        protected abstract Task WriteResponseToOutputStream(Stream stream, ResponseInfo responseInfo, long? contentLength);

        /// <summary>
        /// Disposes the response stream.
        /// </summary>
        protected virtual void DisposeResponseStream()
        {
            if (CompressedStream != null)
            {
                try
                {
                    CompressedStream.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error disposing compressed stream", ex);
                }
            }

            try
            {
                //HttpListenerContext.Response.OutputStream.Dispose();
                HttpListenerContext.Response.Close();
            }
            catch (Exception ex)
            {
                Logger.ErrorException("Error disposing response", ex);
            }
        }

        /// <summary>
        /// Determines whether [is cache valid] [the specified if modified since].
        /// </summary>
        /// <param name="ifModifiedSince">If modified since.</param>
        /// <param name="cacheDuration">Duration of the cache.</param>
        /// <param name="dateModified">The date modified.</param>
        /// <returns><c>true</c> if [is cache valid] [the specified if modified since]; otherwise, <c>false</c>.</returns>
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
        /// <param name="date">The date.</param>
        /// <returns>DateTime.</returns>
        private DateTime NormalizeDateForComparison(DateTime date)
        {
            return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind);
        }

        /// <summary>
        /// Gets the total length of the content.
        /// </summary>
        /// <param name="responseInfo">The response info.</param>
        /// <returns>System.Nullable{System.Int64}.</returns>
        protected virtual long? GetTotalContentLength(ResponseInfo responseInfo)
        {
            return null;
        }

        /// <summary>
        /// Gets the response info.
        /// </summary>
        /// <returns>Task{ResponseInfo}.</returns>
        protected abstract Task<ResponseInfo> GetResponseInfo();

        /// <summary>
        /// Gets a bool query string param.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected bool GetBoolQueryStringParam(string name)
        {
            var val = QueryString[name] ?? string.Empty;

            return val.Equals("1", StringComparison.OrdinalIgnoreCase) || val.Equals("true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The _form values
        /// </summary>
        private Hashtable _formValues;

        /// <summary>
        /// Gets a value from form POST data
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>Task{System.String}.</returns>
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
        /// <param name="request">The request.</param>
        /// <returns>Task{Hashtable}.</returns>
        private async Task<Hashtable> GetFormValues(HttpListenerRequest request)
        {
            var formVars = new Hashtable();

            if (request.HasEntityBody)
            {
                if (request.ContentType.IndexOf("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    using (var requestBody = request.InputStream)
                    {
                        using (var reader = new StreamReader(requestBody, request.ContentEncoding))
                        {
                            var s = await reader.ReadToEndAsync().ConfigureAwait(false);

                            var pairs = s.Split('&');

                            foreach (var pair in pairs)
                            {
                                var index = pair.IndexOf('=');

                                if (index != -1)
                                {
                                    var name = pair.Substring(0, index);
                                    var value = pair.Substring(index + 1);
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

    internal static class SharedLogger
    {
        /// <summary>
        /// The logger
        /// </summary>
        internal static ILogger Logger = LogManager.GetLogger("Http Handler");
    }

    /// <summary>
    /// Class ResponseInfo
    /// </summary>
    public class ResponseInfo
    {
        /// <summary>
        /// Gets or sets the type of the content.
        /// </summary>
        /// <value>The type of the content.</value>
        public string ContentType { get; set; }
        /// <summary>
        /// Gets or sets the etag.
        /// </summary>
        /// <value>The etag.</value>
        public Guid? Etag { get; set; }
        /// <summary>
        /// Gets or sets the date last modified.
        /// </summary>
        /// <value>The date last modified.</value>
        public DateTime? DateLastModified { get; set; }
        /// <summary>
        /// Gets or sets the duration of the cache.
        /// </summary>
        /// <value>The duration of the cache.</value>
        public TimeSpan CacheDuration { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [compress response].
        /// </summary>
        /// <value><c>true</c> if [compress response]; otherwise, <c>false</c>.</value>
        public bool CompressResponse { get; set; }
        /// <summary>
        /// Gets or sets the status code.
        /// </summary>
        /// <value>The status code.</value>
        public int StatusCode { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [supports byte range requests].
        /// </summary>
        /// <value><c>true</c> if [supports byte range requests]; otherwise, <c>false</c>.</value>
        public bool SupportsByteRangeRequests { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseInfo" /> class.
        /// </summary>
        public ResponseInfo()
        {
            CacheDuration = TimeSpan.FromTicks(0);

            CompressResponse = true;

            StatusCode = 200;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is response valid.
        /// </summary>
        /// <value><c>true</c> if this instance is response valid; otherwise, <c>false</c>.</value>
        public bool IsResponseValid
        {
            get
            {
                return StatusCode >= 200 && StatusCode < 300;
            }
        }
    }
}